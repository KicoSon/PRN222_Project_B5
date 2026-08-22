(() => {
    const cfg = window.CHAT_CONFIG;
    if (!cfg || typeof signalR === "undefined") return;

    const list = document.getElementById("message-list");
    const input = document.getElementById("input-message");
    const send = document.getElementById("btn-send");
    const statusBadge = document.getElementById("status-badge");
    const connectionStatus = document.getElementById("connection-status");

    const STATUS_TEXT = { Pending: "Mới nộp", Interview: "Phỏng vấn", Approved: "Đã duyệt nhận", Rejected: "Từ chối" };
    const STATUS_CLASS = { Pending: "badge-pending", Interview: "badge-interview", Approved: "badge-approved", Rejected: "badge-rejected" };
    const token = document.querySelector('#chat-antiforgery-form input[name="__RequestVerificationToken"]')?.value;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    function setConnectionState(text, connected) {
        if (!connectionStatus) return;
        connectionStatus.innerHTML = `<span class="dot"></span> ${escapeHtml(text)}`;
        connectionStatus.classList.toggle("connected", connected);
    }

    function scrollBottom() {
        if (list) list.scrollTop = list.scrollHeight;
    }

    function escapeHtml(value) {
        const div = document.createElement("div");
        div.textContent = value ?? "";
        return div.innerHTML;
    }

    function appendMessage(m) {
        const row = document.createElement("div");
        const mine = Number(m.senderUserId) === Number(cfg.currentUserId);

        if (m.isSystemMessage) {
            row.className = `chat-system-message ${m.isFlagged ? "flagged" : ""}`;
            row.innerHTML = `<i class="bi ${m.isFlagged ? "bi-shield-exclamation" : "bi-info-circle"} me-1"></i>${escapeHtml(m.content)}<span>${escapeHtml(m.createdAt)}</span>`;
        } else {
            row.className = `chat-message-row ${mine ? "mine" : "theirs"}`;
            const reportBtn = !mine && m.chatMessageId
                ? `<button type="button" class="chat-report-btn" title="Báo cáo tin nhắn này" data-message-id="${m.chatMessageId}"><i class="bi bi-flag"></i></button>`
                : "";
            row.innerHTML = `${reportBtn}<div class="chat-bubble"><div class="chat-bubble-content">${escapeHtml(m.content)}</div><div class="chat-bubble-time">${escapeHtml(m.createdAt)}</div></div>`;
        }
        list.appendChild(row);
        scrollBottom();
    }

    async function reportMessage(chatMessageId) {
        if (!token) return;
        const reason = window.prompt("Mô tả ngắn lý do báo cáo (không bắt buộc):", "");
        if (reason === null) return; // người dùng bấm Hủy
        try {
            const response = await fetch("/Chat/ReportMessage", {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": token,
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: new URLSearchParams({ chatMessageId, chatRoomId: cfg.roomId, reason: reason || "" })
            });
            const data = await response.json();
            if (!data.success) throw new Error(data.message || "Không thể gửi báo cáo.");
            alert("Đã gửi báo cáo cho quản trị viên. Cảm ơn bạn!");
        } catch (err) {
            console.error(err);
            alert("Không thể gửi báo cáo, vui lòng thử lại.");
        }
    }

    list?.addEventListener("click", e => {
        const btn = e.target.closest(".chat-report-btn");
        if (btn) reportMessage(btn.dataset.messageId);
    });

    connection.on("ReceiveMessage", appendMessage);

    connection.on("ApplicationStatusChanged", payload => {
        if (!payload) return;
        if (statusBadge) {
            statusBadge.textContent = STATUS_TEXT[payload.status] || payload.status;
            statusBadge.className = `status-pill ${STATUS_CLASS[payload.status] || "bg-secondary"}`;
        }
        if (payload.status === "Rejected") lockComposer("Cuộc trò chuyện đã đóng vì hồ sơ ứng tuyển đã bị từ chối.");
    });

    function lockComposer(message) {
        cfg.locked = true;
        if (input) { input.disabled = true; input.placeholder = "Cuộc trò chuyện đã đóng"; }
        if (send) send.disabled = true;
        document.querySelector(".chat-composer")?.classList.add("disabled");
        if (connectionStatus && message) {
            connectionStatus.outerHTML = `<div class="chat-locked-banner"><i class="bi bi-lock-fill me-1"></i>${escapeHtml(message)}</div>`;
        }
    }

    connection.onreconnecting(() => setConnectionState("Đang kết nối lại...", false));
    connection.onreconnected(async () => {
        setConnectionState("Đã kết nối", true);
        await connection.invoke("JoinRoom", cfg.roomId);
    });
    connection.onclose(() => setConnectionState("Mất kết nối", false));

    async function start() {
        if (cfg.locked) return; // phòng đã đóng/tài khoản bị khóa - không cần realtime gửi tin
        try {
            await connection.start();
            await connection.invoke("JoinRoom", cfg.roomId);
            setConnectionState("Đã kết nối", true);
            scrollBottom();
        } catch (err) {
            console.error(err);
            setConnectionState("Không thể kết nối", false);
            setTimeout(start, 3000);
        }
    }

    async function sendMessage() {
        if (cfg.locked) return;
        const content = input.value.trim();
        if (!content || connection.state !== signalR.HubConnectionState.Connected) return;
        send.disabled = true;
        try {
            await connection.invoke("SendMessage", cfg.roomId, content);
            input.value = "";
            input.focus();
        } catch (err) {
            console.error(err);
            alert(err?.message?.replace(/^.*HubException:\s*/i, "") || "Không thể gửi tin nhắn.");
        } finally {
            send.disabled = false;
        }
    }

    send?.addEventListener("click", sendMessage);
    input?.addEventListener("keydown", e => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    start();
})();
