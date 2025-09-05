// Global variables
let currentUser = null;
let selectedUser = null;
let connection = null;
let users = [];
let conversations = {};
let selectedImage = null;

// API base URL
const API_BASE = '/api';

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    // Check if user is already logged in
    const token = localStorage.getItem('chatToken');
    if (token) {
        currentUser = JSON.parse(localStorage.getItem('chatUser'));
        showChatInterface();
        initializeSignalR();
        loadUsers();
    } else {
        showAuthModal();
    }
});

// Authentication functions
function showAuthModal() {
    const modal = new bootstrap.Modal(document.getElementById('authModal'));
    modal.show();
}

function showLoginForm() {
    document.getElementById('loginForm').style.display = 'block';
    document.getElementById('registerForm').style.display = 'none';
    document.getElementById('authModalTitle').textContent = 'Giriş Yap';
}

function showRegisterForm() {
    document.getElementById('loginForm').style.display = 'none';
    document.getElementById('registerForm').style.display = 'block';
    document.getElementById('authModalTitle').textContent = 'Kayıt Ol';
}

async function login() {
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;

    if (!email || !password) {
        alert('Lütfen tüm alanları doldurun');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/user/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();
        if (response.ok) {
            localStorage.setItem('chatToken', data.token);
            // Yeni: id'yi de kaydet
            currentUser = { id: data.id, email, username: email.split('@')[0] };
            localStorage.setItem('chatUser', JSON.stringify(currentUser));
            
            bootstrap.Modal.getInstance(document.getElementById('authModal')).hide();
            showChatInterface();
            initializeSignalR();
            loadUsers();
        } else {
            alert(data.message || 'Giriş başarısız');
        }
    } catch (error) {
        console.error('Login error:', error);
        alert('Giriş sırasında hata oluştu');
    }
}

async function register() {
    const username = document.getElementById('registerUsername').value;
    const email = document.getElementById('registerEmail').value;
    const password = document.getElementById('registerPassword').value;

    if (!username || !email || !password) {
        alert('Lütfen tüm alanları doldurun');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/user/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, email, password })
        });

        const data = await response.json();
        if (response.ok) {
            alert('Kayıt başarılı! Şimdi giriş yapabilirsiniz.');
            showLoginForm();
        } else {
            alert(data.message || 'Kayıt başarısız');
        }
    } catch (error) {
        console.error('Register error:', error);
        alert('Kayıt sırasında hata oluştu');
    }
}

function logout() {
    localStorage.removeItem('chatToken');
    localStorage.removeItem('chatUser');
    currentUser = null;
    selectedUser = null;
    
    if (connection) {
        connection.stop();
    }
    
    showAuthModal();
}

// UI functions
function showChatInterface() {
    document.getElementById('chatInterface').style.display = 'block';
    document.getElementById('currentUser').textContent = currentUser.username;
    
    // Enable image button by default when chat interface is shown
    document.getElementById('imageButton').disabled = false;
}

// SignalR functions
function initializeSignalR() {
    const token = localStorage.getItem('chatToken');
    
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub", { accessTokenFactory: () => token })
        .build();

    connection.on("ReceiveMessage", (message) => {
        addMessageToUI(message, false);
        updateUnreadCount(message.sender.id);
    });

    connection.on("UserOnline", (userId) => {
        updateUserStatus(userId, true);
    });

    connection.on("UserOffline", (userId) => {
        updateUserStatus(userId, false);
    });

    connection.on("MessageRead", (messageId, userId) => {
        markMessageAsRead(messageId);
    });

    connection.on("UserTyping", (userId) => {
        if (selectedUser && selectedUser.id === userId) {
            showTypingIndicator(userId);
        }
    });

    connection.on("UserStoppedTyping", (userId) => {
        if (selectedUser && selectedUser.id === userId) {
            hideTypingIndicator();
        }
    });

    connection.start()
        .then(() => {
            console.log("SignalR Connected");
            // Join chat with user ID (you'll need to get this from the login response)
            if (currentUser && currentUser.id) {
                connection.invoke("JoinChat", currentUser.id);
            }
        })
        .catch(err => console.error("SignalR Connection Error: ", err));
}

// User management
async function loadUsers() {
    try {
        const response = await fetch(`${API_BASE}/user/users`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('chatToken')}`
            }
        });

        if (response.ok) {
            users = await response.json();
            renderUsers();
        }
    } catch (error) {
        console.error('Load users error:', error);
    }
}

function renderUsers() {
    const usersList = document.getElementById('usersList');
    usersList.innerHTML = '';

    users.forEach(user => {
        const userItem = document.createElement('div');
        userItem.className = 'user-item d-flex align-items-center justify-content-between';
        userItem.onclick = () => selectUser(user);
        
        userItem.innerHTML = `
            <div class="d-flex align-items-center">
                <span class="online-indicator ${user.isOnline ? 'online' : 'offline'}"></span>
                <div>
                    <div class="fw-bold">${user.username}</div>
                    <small class="text-muted">${user.isOnline ? 'Çevrimiçi' : 'Çevrimdışı'}</small>
                </div>
            </div>
            ${user.unreadMessageCount > 0 ? `<span class="unread-badge">${user.unreadMessageCount}</span>` : ''}
        `;
        
        usersList.appendChild(userItem);
    });
}

function selectUser(user) {
    selectedUser = user;
    
    // Update UI
    document.querySelectorAll('.user-item').forEach(item => item.classList.remove('active'));
    event.currentTarget.classList.add('active');
    
    document.getElementById('messageInput').disabled = false;
    document.getElementById('sendButton').disabled = false;
    document.getElementById('imageButton').disabled = false;
    
    // Load conversation
    loadConversation(user.id);
    
    // Clear unread count
    if (user.unreadMessageCount > 0) {
        updateUnreadCount(user.id, 0);
    }
}

// Message functions
async function loadConversation(userId) {
    try {
        const response = await fetch(`${API_BASE}/message/conversation/${userId}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('chatToken')}`
            }
        });

        if (response.ok) {
            const messages = await response.json();
            conversations[userId] = messages;
            renderMessages(messages);
        }
    } catch (error) {
        console.error('Load conversation error:', error);
    }
}

function renderMessages(messages) {
    const messagesArea = document.getElementById('messagesArea');
    messagesArea.innerHTML = '';

    if (messages.length === 0) {
        messagesArea.innerHTML = `
            <div class="text-center text-muted mt-5">
                <i class="fas fa-comments fa-3x mb-3"></i>
                <p>Henüz mesaj yok. Sohbeti başlatın!</p>
            </div>
        `;
        return;
    }

    messages.forEach(message => {
        // Doğru karşılaştırma yapılıyor, ancak currentUser.id eksikti
        addMessageToUI(message, message.sender.id === currentUser.id);
    });

    // Scroll to bottom
    messagesArea.scrollTop = messagesArea.scrollHeight;
}

async function sendMessage() {
    if (!selectedUser) return;

    // If image is selected, upload image instead
    if (selectedImage) {
        uploadImage();
        return;
    }

    const messageInput = document.getElementById('messageInput');
    const content = messageInput.value.trim();
    
    if (!content) return;

    try {
        const response = await fetch(`${API_BASE}/message/send`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('chatToken')}`
            },
            body: JSON.stringify({
                receiverId: selectedUser.id,
                content: content,
                messageType: 0 // Text message
            })
        });

        if (response.ok) {
            const message = await response.json();
            addMessageToUI(message, true);
            messageInput.value = '';
            
            // Send via SignalR
            if (connection) {
                connection.invoke("SendMessage", {
                    receiverId: selectedUser.id,
                    content: content,
                    messageType: 0
                });
            }
        }
    } catch (error) {
        console.error('Send message error:', error);
        alert('Mesaj gönderilemedi');
    }
}

function addMessageToUI(message, isSent) {
    const messagesArea = document.getElementById('messagesArea');
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${isSent ? 'sent' : 'received'}`;
    
    let messageContent = '';
    if (message.messageType === 1) { // Image message
        messageContent = `
            <div class="image-message">
                <img src="${message.imageUrl}" alt="${message.imageFileName || 'Image'}" 
                     class="message-image" onclick="openImageModal('${message.imageUrl}', '${message.imageFileName || 'Image'}')">
                ${message.content ? `<div class="mt-2">${message.content}</div>` : ''}
            </div>
        `;
    } else { // Text message
        messageContent = `<div class="message-content">${message.content}</div>`;
    }
    
    messageDiv.innerHTML = `
        ${messageContent}
        <small class="message-time">${new Date(message.sentAt).toLocaleTimeString()}</small>
    `;
    
    messagesArea.appendChild(messageDiv);
    messagesArea.scrollTop = messagesArea.scrollHeight;
}

// Utility functions
function updateUserStatus(userId, isOnline) {
    const user = users.find(u => u.id === userId);
    if (user) {
        user.isOnline = isOnline;
        user.lastSeen = new Date();
        renderUsers();
    }
}

function updateUnreadCount(userId, count = null) {
    const user = users.find(u => u.id === userId);
    if (user) {
        if (count !== null) {
            user.unreadMessageCount = count;
        } else {
            user.unreadMessageCount++;
        }
        renderUsers();
    }
}

function markMessageAsRead(messageId) {
    // Update UI to show message as read
    const messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
    if (messageElement) {
        messageElement.classList.add('read');
    }
}

function showTypingIndicator(userId) {
    const typingIndicator = document.getElementById('typingIndicator');
    const user = users.find(u => u.id === userId);
    if (user) {
        typingIndicator.textContent = `${user.username} yazıyor...`;
        typingIndicator.style.display = 'block';
    }
}

function hideTypingIndicator() {
    document.getElementById('typingIndicator').style.display = 'none';
}

// Typing indicators
let typingTimer;
document.getElementById('messageInput').addEventListener('input', function() {
    if (selectedUser && connection) {
        clearTimeout(typingTimer);
        connection.invoke("Typing", selectedUser.id);
        
        typingTimer = setTimeout(() => {
            connection.invoke("StopTyping", selectedUser.id);
        }, 1000);
    }
});

// Image upload functions
function selectImage() {
    document.getElementById('imageInput').click();
}

function handleImageSelect() {
    const input = document.getElementById('imageInput');
    const file = input.files[0];
    
    if (file) {
        // Validate file type
        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
        if (!allowedTypes.includes(file.type)) {
            alert('Geçersiz dosya türü. Sadece JPEG, PNG, GIF ve WebP dosyaları kabul edilir.');
            return;
        }
        
        // Validate file size (5MB)
        if (file.size > 5 * 1024 * 1024) {
            alert('Dosya boyutu çok büyük. Maksimum 5MB kabul edilir.');
            return;
        }
        
        selectedImage = file;
        showImagePreview(file);
    }
}

function showImagePreview(file) {
    const preview = document.getElementById('imagePreview');
    const previewImage = document.getElementById('previewImage');
    const previewFileName = document.getElementById('previewFileName');
    const previewFileSize = document.getElementById('previewFileSize');
    
    // Create object URL for preview
    const objectUrl = URL.createObjectURL(file);
    previewImage.src = objectUrl;
    previewFileName.textContent = file.name;
    previewFileSize.textContent = formatFileSize(file.size);
    
    preview.style.display = 'block';
    document.getElementById('messageInput').placeholder = 'Resim açıklaması (isteğe bağlı)...';
}

function cancelImageUpload() {
    selectedImage = null;
    document.getElementById('imagePreview').style.display = 'none';
    document.getElementById('imageInput').value = '';
    document.getElementById('messageInput').placeholder = 'Mesajınızı yazın...';
    
    // Revoke object URL to free memory
    const previewImage = document.getElementById('previewImage');
    if (previewImage.src.startsWith('blob:')) {
        URL.revokeObjectURL(previewImage.src);
    }
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

async function uploadImage() {
    if (!selectedImage || !selectedUser) return;
    
    const formData = new FormData();
    formData.append('image', selectedImage);
    formData.append('receiverId', selectedUser.id);
    
    const messageInput = document.getElementById('messageInput');
    const caption = messageInput.value.trim();
    
    try {
        // Show loading state
        const imageButton = document.getElementById('imageButton');
        const sendButton = document.getElementById('sendButton');
        imageButton.disabled = true;
        sendButton.disabled = true;
        imageButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        
        const response = await fetch(`${API_BASE}/message/upload-image`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('chatToken')}`
            },
            body: formData
        });
        
        if (response.ok) {
            const data = await response.json();
            
            // Add caption if provided
            if (caption) {
                await fetch(`${API_BASE}/message/send`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${localStorage.getItem('chatToken')}`
                    },
                    body: JSON.stringify({
                        receiverId: selectedUser.id,
                        content: caption,
                        messageType: 1,
                        imageUrl: data.imageUrl,
                        imageFileName: selectedImage.name
                    })
                });
            }
            
            // Add to UI
            addMessageToUI(data.message, true);
            
            // Send via SignalR
            if (connection) {
                connection.invoke("SendMessage", {
                    receiverId: selectedUser.id,
                    content: caption,
                    messageType: 1,
                    imageUrl: data.imageUrl,
                    imageFileName: selectedImage.name
                });
            }
            
            // Clear input and preview
            messageInput.value = '';
            cancelImageUpload();
        } else {
            const error = await response.json();
            alert(error.message || 'Resim yüklenemedi');
        }
    } catch (error) {
        console.error('Image upload error:', error);
        alert('Resim yükleme sırasında hata oluştu');
    } finally {
        // Reset buttons
        const imageButton = document.getElementById('imageButton');
        const sendButton = document.getElementById('sendButton');
        imageButton.disabled = false;
        sendButton.disabled = false;
        imageButton.innerHTML = '<i class="fas fa-image"></i>';
    }
}

function openImageModal(imageUrl, fileName) {
    // Create modal for full-size image view
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.innerHTML = `
        <div class="modal-dialog modal-lg modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">${fileName}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body text-center">
                    <img src="${imageUrl}" alt="${fileName}" class="img-fluid">
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();
    
    // Remove modal from DOM when hidden
    modal.addEventListener('hidden.bs.modal', () => {
        document.body.removeChild(modal);
    });
}

// Enter key to send message
document.getElementById('messageInput').addEventListener('keypress', function(e) {
    if (e.key === 'Enter') {
        if (selectedImage) {
            uploadImage();
        } else {
            sendMessage();
        }
    }
});