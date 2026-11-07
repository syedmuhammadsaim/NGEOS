/**
 * NGEOS AI Chatbot Client Script
 * Handles real-time messaging, multi-provider fallback display,
 * live database grounding responses, and smooth scrolling.
 */
(function () {
    "use strict";

    const STORAGE_KEY = "NGEOS_AI_CHAT_HISTORY";
    const WINDOW_STATE_KEY = "NGEOS_AI_CHAT_OPEN";

    // DOM Elements
    let container, toggleBtn, chatWindow, minimizeBtn, clearBtn;
    let messagesContainer, chatForm, chatInput, sendBtn, typingIndicator;
    let suggestionsContainer, providerBadge;

    // State
    let conversationHistory = [];
    let isWaitingForResponse = false;

    document.addEventListener("DOMContentLoaded", initChatbot);

    function initChatbot() {
        container = document.getElementById("ngeos-chatbot-container");
        if (!container) return;

        toggleBtn = document.getElementById("ngeos-chatbot-toggle-btn");
        chatWindow = document.getElementById("ngeos-chatbot-window");
        minimizeBtn = document.getElementById("ngeos-chat-minimize-btn");
        clearBtn = document.getElementById("ngeos-chat-clear-btn");
        messagesContainer = document.getElementById("ngeos-chat-messages");
        chatForm = document.getElementById("ngeos-chat-form");
        chatInput = document.getElementById("ngeos-chat-input");
        sendBtn = document.getElementById("ngeos-chat-send-btn");
        typingIndicator = document.getElementById("ngeos-chat-typing-indicator");
        suggestionsContainer = document.getElementById("ngeos-chat-suggestions");
        providerBadge = document.getElementById("ngeos-chat-provider-badge");

        // Event Listeners
        toggleBtn?.addEventListener("click", toggleChatWindow);
        minimizeBtn?.addEventListener("click", closeChatWindow);
        clearBtn?.addEventListener("click", clearConversation);
        chatForm?.addEventListener("submit", handleFormSubmit);

        // Enter key to send
        chatInput?.addEventListener("keydown", function (e) {
            if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                handleFormSubmit(e);
            }
        });

        // Delegate suggestion chip clicks
        container.addEventListener("click", function (e) {
            const chip = e.target.closest(".suggestion-chip");
            if (chip && !isWaitingForResponse) {
                const prompt = chip.getAttribute("data-prompt") || chip.innerText.trim();
                if (prompt) {
                    sendUserMessage(prompt);
                }
            }
        });

        // Load saved session history if available
        loadSessionHistory();

        // Restore window open state if previously opened in this session
        if (sessionStorage.getItem(WINDOW_STATE_KEY) === "true") {
            openChatWindow();
        }

        // Fetch operational status quietly in background
        checkProviderStatus();
    }

    function toggleChatWindow() {
        if (chatWindow.classList.contains("open")) {
            closeChatWindow();
        } else {
            openChatWindow();
        }
    }

    function openChatWindow() {
        chatWindow.classList.add("open");
        toggleBtn.classList.add("active");
        toggleBtn.setAttribute("aria-expanded", "true");
        sessionStorage.setItem(WINDOW_STATE_KEY, "true");
        setTimeout(() => {
            chatInput?.focus();
            scrollToBottom();
        }, 150);
    }

    function closeChatWindow() {
        chatWindow.classList.remove("open");
        toggleBtn.classList.remove("active");
        toggleBtn.setAttribute("aria-expanded", "false");
        sessionStorage.setItem(WINDOW_STATE_KEY, "false");
    }

    function handleFormSubmit(e) {
        if (e) e.preventDefault();
        if (isWaitingForResponse) return;

        const text = chatInput.value.trim();
        if (!text) return;

        sendUserMessage(text);
        chatInput.value = "";
    }

    async function sendUserMessage(messageText) {
        if (!messageText || isWaitingForResponse) return;

        // 1. Render user message bubble
        renderMessage("user", messageText);

        // 2. Add to conversation history
        conversationHistory.push({ role: "user", content: messageText });
        saveSessionHistory();

        // 3. Set loading state
        setLoadingState(true);
        scrollToBottom();

        try {
            const response = await fetch("/api/chatbot/message", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Accept": "application/json"
                },
                body: JSON.stringify({
                    message: messageText,
                    conversationHistory: conversationHistory.slice(-8)
                })
            });

            if (!response.ok) {
                throw new Error("HTTP error " + response.status);
            }

            const data = await response.json();

            // 4. Render Bot Reply
            const reply = data.reply || "I am unable to answer at this moment.";
            const provider = data.providerUsed || "AI";
            renderMessage("bot", reply, provider);

            // 5. Update active history
            conversationHistory.push({ role: "assistant", content: reply });
            saveSessionHistory();

            // 6. Update suggestion chips if provided
            if (data.suggestedActions && data.suggestedActions.length > 0) {
                renderSuggestionChips(data.suggestedActions);
            }

            // 7. Update header provider badge
            if (providerBadge && provider && provider !== "None") {
                providerBadge.textContent = provider === "Groq" ? "Groq Llama 3.3" : "Gemini 2.0";
            }
        } catch (error) {
            console.error("Chatbot request failed:", error);
            renderMessage(
                "bot", 
                "Sorry, the AI assistant is temporarily unavailable. Please check your connection or try again later.", 
                "Fallback"
            );
        } finally {
            setLoadingState(false);
            scrollToBottom();
        }
    }

    function renderMessage(role, text, provider = null) {
        const msgDiv = document.createElement("div");
        msgDiv.className = `chat-msg chat-msg-${role}`;

        const timeStr = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        if (role === "user") {
            msgDiv.innerHTML = `
                <div class="msg-content">
                    <div class="msg-bubble">${escapeHtml(text)}</div>
                    <span class="msg-timestamp">${timeStr}</span>
                </div>
            `;
        } else {
            const formattedContent = parseMarkdown(text);
            const providerHtml = provider && provider !== "None" 
                ? `<span class="provider-badge-indicator"><i class="bi bi-cpu-fill text-warning"></i> via ${escapeHtml(provider)}</span>` 
                : '';

            msgDiv.innerHTML = `
                <div class="msg-avatar"><img src="/images/ngeos-logo.svg" alt="NGEOS" class="msg-avatar-img" /></div>
                <div class="msg-content">
                    <div class="msg-bubble">${formattedContent}</div>
                    <div class="d-flex align-items-center justify-content-between">
                        <span class="msg-timestamp">${timeStr}</span>
                        ${providerHtml}
                    </div>
                </div>
            `;
        }

        // Insert before typing indicator
        messagesContainer.insertBefore(msgDiv, typingIndicator);
    }

    function renderSuggestionChips(actions) {
        if (!suggestionsContainer) return;
        const chipsContainer = suggestionsContainer.querySelector(".suggestions-chips");
        if (!chipsContainer) return;

        chipsContainer.innerHTML = "";
        actions.forEach(action => {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "suggestion-chip";
            btn.setAttribute("data-prompt", action);
            btn.innerHTML = `<i class="bi bi-arrow-right-short me-1"></i> ${escapeHtml(action)}`;
            chipsContainer.appendChild(btn);
        });
    }

    function setLoadingState(loading) {
        isWaitingForResponse = loading;
        if (loading) {
            typingIndicator?.classList.remove("d-none");
            sendBtn?.setAttribute("disabled", "disabled");
        } else {
            typingIndicator?.classList.add("d-none");
            sendBtn?.removeAttribute("disabled");
            chatInput?.focus();
        }
    }

    function clearConversation() {
        if (confirm("Clear conversation history?")) {
            conversationHistory = [];
            sessionStorage.removeItem(STORAGE_KEY);

            // Remove all messages except welcome message and typing indicator
            const msgs = messagesContainer.querySelectorAll(".chat-msg:not(:first-child):not(#ngeos-chat-typing-indicator)");
            msgs.forEach(m => m.remove());

            // Reset suggestions
            renderSuggestionChips([
                "Active Causes",
                "How to Donate?",
                "Total Funds Raised",
                "Programmes",
                "Volunteer Jobs"
            ]);

            chatInput?.focus();
        }
    }

    function scrollToBottom() {
        if (!messagesContainer) return;
        messagesContainer.scrollTo({
            top: messagesContainer.scrollHeight,
            behavior: "smooth"
        });
    }

    function saveSessionHistory() {
        try {
            sessionStorage.setItem(STORAGE_KEY, JSON.stringify(conversationHistory));
        } catch (e) {
            // Ignore storage quota errors
        }
    }

    function loadSessionHistory() {
        try {
            const saved = sessionStorage.getItem(STORAGE_KEY);
            if (saved) {
                const parsed = JSON.parse(saved);
                if (Array.isArray(parsed) && parsed.length > 0) {
                    conversationHistory = parsed;
                    parsed.forEach(item => {
                        renderMessage(item.role === "user" ? "user" : "bot", item.content);
                    });
                }
            }
        } catch (e) {
            conversationHistory = [];
        }
    }

    async function checkProviderStatus() {
        try {
            const res = await fetch("/api/chatbot/status");
            if (res.ok) {
                const status = await res.json();
                if (providerBadge) {
                    if (status.isGroqConfigured && status.isGeminiConfigured) {
                        providerBadge.textContent = "Groq + Gemini";
                    } else if (status.isGroqConfigured) {
                        providerBadge.textContent = "Groq Active";
                    } else if (status.isGeminiConfigured) {
                        providerBadge.textContent = "Gemini Active";
                    } else {
                        providerBadge.textContent = "Standby";
                    }
                }
            }
        } catch (e) {
            // Non-blocking status check
        }
    }

    function escapeHtml(str) {
        if (!str) return "";
        return str
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // Lightweight markdown parser for bold, italic, bullet points, and hyperlinks
    function parseMarkdown(text) {
        if (!text) return "";
        let html = escapeHtml(text);

        // Bold: **text** or __text__
        html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

        // Italic: *text*
        html = html.replace(/\*(.*?)\*/g, '<em>$1</em>');

        // Markdown links: [text](url)
        html = html.replace(/\[(.*?)\]\((.*?)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');

        // Convert bullet points: lines starting with "- " or "* "
        const lines = html.split('\n');
        let inList = false;
        let formatted = [];

        for (let line of lines) {
            const trimmed = line.trim();
            if (trimmed.startsWith('- ') || trimmed.startsWith('* ')) {
                if (!inList) {
                    formatted.push('<ul>');
                    inList = true;
                }
                formatted.push(`<li>${trimmed.substring(2)}</li>`);
            } else {
                if (inList) {
                    formatted.push('</ul>');
                    inList = false;
                }
                if (trimmed.length > 0) {
                    formatted.push(`<p class="mb-1">${line}</p>`);
                }
            }
        }
        if (inList) {
            formatted.push('</ul>');
        }

        return formatted.join('');
    }

})();
