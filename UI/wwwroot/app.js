// Scroll to bottom of messages container
function scrollToBottom() {
    const messageArea = document.getElementById('messageArea');
    if (messageArea) {
        messageArea.scrollTop = messageArea.scrollHeight;
    }
}

// Native send message button handler
window.sendMessageNative = async function() {
    console.log('=== sendMessageNative START ===');
    const textarea = document.querySelector('textarea');
    if (!textarea || !textarea.value.trim()) {
        console.log('No message in textarea');
        return;
    }
    
    const message = textarea.value;
    console.log('Message to send:', message);
    textarea.value = '';
    
    try {
        console.log('Component available?', !!window.chatComponent);
        
        if (!window.chatComponent) {
            console.error('ERROR: Component not registered!');
            alert('Component not ready. Please refresh page.');
            return;
        }
        
        console.log('=== Calling AddMessage(user) ===');
        const addUserResult = await window.chatComponent.invokeMethodAsync('AddMessage', message, true);
        console.log('AddMessage(user) result:', addUserResult);
        
        console.log('=== Calling SetThinking(true) ===');
        const setThinkingResult = await window.chatComponent.invokeMethodAsync('SetThinking', true);
        console.log('SetThinking(true) result:', setThinkingResult);
        
        scrollToBottom();
        
        console.log('=== Fetching from API ===');
        const response = await fetch('/api/rag/answer', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ Query: message })
        });
        
        console.log('API response status:', response.status);
        
        if (response.ok) {
            const data = await response.json();
            console.log('API response data:', data);
            
            console.log('=== Calling AddMessage(assistant) ===');
            const addAssistantResult = await window.chatComponent.invokeMethodAsync('AddMessage', data.answer || 'Cevap alınamadı', false);
            console.log('AddMessage(assistant) result:', addAssistantResult);
            
            console.log('=== Calling SetThinking(false) ===');
            const clearThinkingResult = await window.chatComponent.invokeMethodAsync('SetThinking', false);
            console.log('SetThinking(false) result:', clearThinkingResult);
        } else {
            const errorText = await response.text();
            console.error('API Error:', errorText);
            
            console.log('=== Calling AddMessage(error) ===');
            await window.chatComponent.invokeMethodAsync('AddMessage', 'Hata: ' + response.statusText, false);
            await window.chatComponent.invokeMethodAsync('SetThinking', false);
        }
        
        scrollToBottom();
        console.log('=== sendMessageNative END (success) ===');
    } catch (error) {
        console.error('=== CATCH ERROR ===', error);
        console.error('Error type:', error.constructor.name);
        console.error('Error message:', error.message);
        console.error('Error stack:', error.stack);
        
        try {
            if (window.chatComponent) {
                await window.chatComponent.invokeMethodAsync('AddMessage', 'Hata: ' + error.message, false);
                await window.chatComponent.invokeMethodAsync('SetThinking', false);
            }
        } catch (e) {
            console.error('Failed to call component error handler:', e);
        }
    }
};

// Function to register the Chat component with JS
window.registerChatComponent = function(component) {
    window.chatComponent = component;
    console.log('=== Chat component registered ===');
    console.log('Component object:', component);
    console.log('Component type:', typeof component);
    console.log('Component._id:', component._id);
    console.log('Has invokeMethodAsync:', typeof component.invokeMethodAsync === 'function');
};

// Test if component is available
window.testComponent = function() {
    if (window.chatComponent) {
        console.log('Component is available');
        console.log('Methods available:', Object.getOwnPropertyNames(window.chatComponent));
    } else {
        console.log('Component is NOT available');
    }
};





