"use strict";

document.querySelectorAll("form[data-confirm]").forEach(form => {
    form.addEventListener("submit", event => {
        if (!window.confirm(form.dataset.confirm)) event.preventDefault();
    });
});

const editor = document.getElementById("post-editor");
if (editor) {
    document.querySelectorAll("[data-add-tag]").forEach(button => {
        button.addEventListener("click", () => {
            const input = editor.querySelector('[name="Input.Tags"]');
            const tags = input.value.split(/[,，]/u).map(tag => tag.trim()).filter(Boolean);
            if (!tags.some(tag => tag.toUpperCase() === button.dataset.addTag.toUpperCase())) {
                tags.push(button.dataset.addTag);
                input.value = tags.join(", ");
            }
        });
    });
    const previewButton = document.getElementById("preview-button");
    previewButton.addEventListener("click", async () => {
        const status = document.getElementById("preview-status");
        status.textContent = "";
        previewButton.disabled = true;
        try {
            const response = await fetch(editor.dataset.previewUrl, {
                method: "POST",
                body: new FormData(editor),
                credentials: "same-origin",
                headers: { "Accept": "application/json" }
            });
            if (!response.ok || !response.headers.get("content-type")?.includes("application/json")) {
                throw new Error("预览失败，请确认登录状态后重试。");
            }
            const result = await response.json();
            document.getElementById("markdown-preview").innerHTML = result.html;
        } catch (error) {
            status.textContent = error.message || "预览失败，请稍后重试。";
        } finally {
            previewButton.disabled = false;
        }
    });
}
