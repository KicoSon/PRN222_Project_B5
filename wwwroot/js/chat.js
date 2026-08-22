(() => {
    const cfg = window.CHAT_CONFIG;
    if (!cfg || typeof signalR === "undefined") return;

    const list = document.getElementById("message-list");
    const input = document.getElementById("input-message");
    const send = document.getElementById("btn-send");
    const status = document.getElementById("select-status");
    const connectionStatus = document.getElementById("connection-status");
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
            row.innerHTML = `<div class="chat-bubble"><div class="chat-bubble-content">${escapeHtml(m.content)}</div><div class="chat-bubble-time">${escapeHtml(m.createdAt)}</div></div>`;
        }
        list.appendChild(row);
        scrollBottom();
    }

    connection.on("ReceiveMessage", appendMessage);

    connection.on("ApplicationStatusChanged", payload => {
        if (!status || !payload) return;
        status.value = payload.status;
    });

    connection.onreconnecting(() => setConnectionState("Đang kết nối lại...", false));
    connection.onreconnected(async () => {
        setConnectionState("Đã kết nối", true);
        await connection.invoke("JoinRoom", cfg.roomId);
    });
    connection.onclose(() => setConnectionState("Mất kết nối", false));

    async function start() {
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
        const content = input.value.trim();
        if (!content || connection.state !== signalR.HubConnectionState.Connected) return;
        send.disabled = true;
        try {
            await connection.invoke("SendMessage", cfg.roomId, content);
            input.value = "";
            input.focus();
        } catch (err) {
            console.error(err);
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

    status?.addEventListener("change", async () => {
        if (!token) return;
        status.disabled = true;
        try {
            const response = await fetch("/Chat/UpdateStatusInChat", {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": token,
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: new URLSearchParams({
                    chatRoomId: cfg.roomId,
                    applicationId: cfg.applicationId,
                    status: status.value
                })
            });
            const data = await response.json();
            if (!data.success) throw new Error(data.message || "Không thể cập nhật trạng thái");
        } catch (err) {
            console.error(err);
            alert("Không thể cập nhật trạng thái ứng tuyển.");
        } finally {
            status.disabled = false;
        }
    });

    start();
})();
