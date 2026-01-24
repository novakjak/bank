const status = document.getElementById("status");
const health = document.getElementById("health");
const connections = document.getElementById("connections");
const total = document.getElementById("total");
const proxy = document.getElementById("proxy");
const errors = document.getElementById("errors");
const persistence = document.getElementById("persistence");
const updated = document.getElementById("updated");

const commands = document.getElementById("commands");
const reset = document.getElementById("reset");
const shutdown = document.getElementById("shutdown");

async function loadStatus() {
    const r = await fetch("/api/status");

    if (!r.ok) {
        status.textContent = "OFFLINE";
        status.className = "dead";
        shutdown.disabled = true;
        return;
    }

    const s = await r.json();

    if (!s.alive) {
        status.textContent = "OFFLINE";
        status.className = "dead";
        shutdown.disabled = true;
        return;
    }

    status.textContent = "RUNNING";
    status.className = "ok";

    health.textContent = s.health;
    connections.textContent = s.connections;
    total.textContent = s.total;
    proxy.textContent = s.proxy;
    errors.textContent = s.errors;
    persistence.textContent = s.persistence;
    updated.textContent = new Date(s.createdAt).toLocaleTimeString();

    shutdown.disabled = false;
}


async function loadCommands() {
    const r = await fetch("/api/commands");
    if (!r.ok) return;

    const data = await r.json();
    commands.innerHTML = "";

    for (const c of data) {
        commands.innerHTML += `
            <tr>
                <td>${c.name}</td>
                <td>${c.count}</td>
                <td>${c.errors}</td>
                <td>${c.avg.toFixed(2)}</td>
            </tr>`;
    }
}

reset.onclick = async () => {
    if (!confirm("Resetovat statistiky?")) return;
    await fetch("/api/reset-commands", { method: "POST" });
};

shutdown.onclick = async () => {
    if (!confirm("Opravdu vypnout node?")) return;
    await fetch("/api/shutdown", { method: "POST" });
};

setInterval(() => {
    loadStatus();
    loadCommands();
}, 3000);

loadStatus();
loadCommands();
