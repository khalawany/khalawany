(() => {
    const selectBtn   = document.getElementById('selectScreen');
    const startBtn    = document.getElementById('startScreenRec');
    const stopBtn     = document.getElementById('stopScreenRec');
    const statusEl    = document.getElementById('screenStatus');
    const dotEl       = document.getElementById('screenDot');
    const previewEl   = document.getElementById('screenPreview');
    const overlayEl   = document.getElementById('cropOverlay');
    const includeMic  = document.getElementById('includeMic');
    const clearCropBtn = document.getElementById('clearCrop');
    const noScreenMsg  = document.getElementById('noScreenMsg');

    if (!selectBtn) return;

    if (!navigator.mediaDevices?.getDisplayMedia) {
        selectBtn.disabled = true;
        startBtn.disabled  = true;
        if (noScreenMsg) noScreenMsg.style.display = 'block';
        if (statusEl) statusEl.textContent = '';
        return;
    }

    let screenStream = null;
    let micStream    = null;
    let recorder     = null;
    let chunks       = [];
    let animId       = null;

    // Crop selection (in video-pixel space)
    let crop     = null;  // { x, y, w, h } or null = full area
    let dragging = false;
    let dragStart = { x: 0, y: 0 };

    function setStatus(msg, dot) {
        if (statusEl) statusEl.textContent = msg;
        if (dotEl)    dotEl.className = 'rec-dot' + (dot ? ' ' + dot : '');
    }

    // ── Crop overlay helpers ──────────────────────────────────────────────

    function syncOverlaySize() {
        if (!overlayEl || !previewEl) return;
        overlayEl.width  = previewEl.clientWidth;
        overlayEl.height = previewEl.clientHeight;
    }

    function toVideoCoords(clientX, clientY) {
        const rect   = previewEl.getBoundingClientRect();
        const scaleX = (previewEl.videoWidth  || previewEl.clientWidth)  / rect.width;
        const scaleY = (previewEl.videoHeight || previewEl.clientHeight) / rect.height;
        return {
            x: Math.round(Math.max(0, Math.min((clientX - rect.left) * scaleX, previewEl.videoWidth))),
            y: Math.round(Math.max(0, Math.min((clientY - rect.top)  * scaleY, previewEl.videoHeight)))
        };
    }

    function drawOverlay() {
        if (!overlayEl) return;
        const ctx = overlayEl.getContext('2d');
        ctx.clearRect(0, 0, overlayEl.width, overlayEl.height);
        if (!crop) return;

        const rect   = previewEl.getBoundingClientRect();
        const scaleX = rect.width  / (previewEl.videoWidth  || 1);
        const scaleY = rect.height / (previewEl.videoHeight || 1);

        const dx = crop.x * scaleX, dy = crop.y * scaleY;
        const dw = crop.w * scaleX, dh = crop.h * scaleY;

        // Dim area outside selection
        ctx.fillStyle = 'rgba(0,0,0,0.5)';
        ctx.fillRect(0, 0, overlayEl.width, overlayEl.height);
        ctx.clearRect(dx, dy, dw, dh);

        // Selection border
        ctx.strokeStyle = '#6366f1';
        ctx.lineWidth   = 2;
        ctx.strokeRect(dx, dy, dw, dh);

        // Corner handles
        const hs = 8;
        ctx.fillStyle = '#6366f1';
        [[dx, dy], [dx + dw - hs, dy], [dx, dy + dh - hs], [dx + dw - hs, dy + dh - hs]]
            .forEach(([fx, fy]) => ctx.fillRect(fx, fy, hs, hs));

        // Size label
        ctx.fillStyle = 'rgba(99,102,241,0.9)';
        ctx.fillRect(dx, dy - 20, 90, 18);
        ctx.fillStyle = '#fff';
        ctx.font = '11px monospace';
        ctx.fillText(`${crop.w} × ${crop.h}`, dx + 4, dy - 6);
    }

    // Mouse events for drag-to-select crop region
    overlayEl.addEventListener('mousedown', e => {
        if (!screenStream) return;
        dragging  = true;
        dragStart = toVideoCoords(e.clientX, e.clientY);
        crop      = null;
        drawOverlay();
    });

    overlayEl.addEventListener('mousemove', e => {
        if (!dragging) return;
        const cur = toVideoCoords(e.clientX, e.clientY);
        const x = Math.min(dragStart.x, cur.x);
        const y = Math.min(dragStart.y, cur.y);
        const w = Math.abs(cur.x - dragStart.x);
        const h = Math.abs(cur.y - dragStart.y);
        if (w > 4 && h > 4) crop = { x, y, w, h };
        drawOverlay();
    });

    overlayEl.addEventListener('mouseup', () => {
        dragging = false;
        if (crop && (crop.w < 20 || crop.h < 20)) crop = null;
        drawOverlay();
        if (clearCropBtn) clearCropBtn.style.display = crop ? 'inline-flex' : 'none';
    });

    clearCropBtn?.addEventListener('click', () => {
        crop = null;
        drawOverlay();
        clearCropBtn.style.display = 'none';
    });

    window.addEventListener('resize', () => { syncOverlaySize(); drawOverlay(); });

    // ── Select screen / window / tab ─────────────────────────────────────

    selectBtn.addEventListener('click', async () => {
        try {
            // Stop any existing stream first
            screenStream?.getTracks().forEach(t => t.stop());

            screenStream = await navigator.mediaDevices.getDisplayMedia({
                video: { frameRate: { ideal: 30, max: 60 } },
                audio: false   // mic handled separately for quality
            });

            previewEl.srcObject = screenStream;
            previewEl.style.display = 'block';
            await new Promise(resolve => { previewEl.onloadedmetadata = resolve; });

            // Show crop overlay
            overlayEl.style.display = 'block';
            syncOverlaySize();
            crop = null;
            drawOverlay();
            if (clearCropBtn) clearCropBtn.style.display = 'none';

            startBtn.disabled = false;
            setStatus('Screen selected — drag to set crop region, then press Start.', '');

            // When the user stops sharing from the browser UI
            screenStream.getVideoTracks()[0].addEventListener('ended', () => {
                if (recorder?.state === 'recording') recorder.stop();
                previewEl.srcObject = null;
                previewEl.style.display = 'none';
                overlayEl.style.display = 'none';
                screenStream = null;
                startBtn.disabled = true;
                stopBtn.disabled  = true;
                setStatus('Screen share ended.', '');
            });
        } catch (err) {
            const msg = err.name === 'NotAllowedError'
                ? 'Permission denied — please allow screen sharing and try again.'
                : 'Could not capture screen: ' + err.message;
            setStatus(msg, '');
        }
    });

    // ── MIME type picker ──────────────────────────────────────────────────

    function getBestMime() {
        const types = [
            'video/webm;codecs=vp9,opus',
            'video/webm;codecs=vp8,opus',
            'video/webm',
            'video/mp4'
        ];
        return types.find(t => MediaRecorder.isTypeSupported(t)) ?? '';
    }

    // ── Start recording ───────────────────────────────────────────────────

    startBtn.addEventListener('click', async () => {
        if (!screenStream) return;

        try {
            // Acquire mic
            if (includeMic?.checked) {
                try {
                    micStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
                } catch {
                    setStatus('Mic denied — recording screen without audio.', '');
                }
            }

            const vTrack   = screenStream.getVideoTracks()[0];
            const settings = vTrack.getSettings();
            const srcW     = settings.width  || previewEl.videoWidth;
            const srcH     = settings.height || previewEl.videoHeight;

            // Output dimensions: use crop if set, else full resolution
            const outW = crop ? crop.w : srcW;
            const outH = crop ? crop.h : srcH;

            // Off-screen canvas for frame cropping / passthrough
            const canvas = document.createElement('canvas');
            canvas.width  = outW;
            canvas.height = outH;
            const ctx = canvas.getContext('2d');

            // Draw loop — copies (cropped) video frames to canvas at ~30 fps
            function drawFrame() {
                if (crop) {
                    ctx.drawImage(previewEl, crop.x, crop.y, crop.w, crop.h, 0, 0, outW, outH);
                } else {
                    ctx.drawImage(previewEl, 0, 0, outW, outH);
                }
                animId = requestAnimationFrame(drawFrame);
            }
            drawFrame();

            // Build combined stream: canvas video + optional mic audio
            const combined = canvas.captureStream(30);
            micStream?.getAudioTracks().forEach(t => combined.addTrack(t));

            const mime    = getBestMime();
            const options = mime ? { mimeType: mime } : {};
            recorder      = new MediaRecorder(combined, options);
            chunks        = [];

            recorder.ondataavailable = e => { if (e.data.size > 0) chunks.push(e.data); };

            recorder.onstop = () => {
                cancelAnimationFrame(animId);
                animId = null;

                const blob = new Blob(chunks, { type: recorder.mimeType });
                const ext  = recorder.mimeType.includes('mp4') ? '.mp4'
                           : recorder.mimeType.includes('ogg') ? '.ogg'
                           : '.webm';
                const file = new File([blob], `screen-${Date.now()}${ext}`, { type: recorder.mimeType });

                const dt = new DataTransfer();
                dt.items.add(file);
                const fileInput = document.getElementById('fileInput');
                if (fileInput) {
                    fileInput.files = dt.files;
                    const chosenFile = document.getElementById('chosenFile');
                    const sizeStr    = (file.size / (1024 * 1024)).toFixed(1) + ' MB';
                    if (chosenFile) chosenFile.textContent = `${file.name} — ${sizeStr}`;
                    // Show the file zone feedback
                    const zoneIcon = document.getElementById('zoneIcon');
                    if (zoneIcon) zoneIcon.textContent = '🎬';
                }

                micStream?.getTracks().forEach(t => t.stop());
                micStream = null;
                screenStream?.getTracks().forEach(t => t.stop());
                screenStream = null;

                previewEl.srcObject = null;
                previewEl.style.display = 'none';
                overlayEl.style.display = 'none';
                crop = null;
                if (clearCropBtn) clearCropBtn.style.display = 'none';

                setStatus('Screen recording saved — ready to submit.', 'done');
                selectBtn.disabled = false;
                startBtn.disabled  = true;
                stopBtn.disabled   = true;
            };

            recorder.start(250);
            setStatus('Recording…', 'live');
            selectBtn.disabled = true;
            startBtn.disabled  = true;
            stopBtn.disabled   = false;

        } catch (err) {
            cancelAnimationFrame(animId);
            setStatus('Recording failed: ' + err.message, '');
        }
    });

    // ── Stop recording ────────────────────────────────────────────────────

    stopBtn.addEventListener('click', () => {
        if (recorder?.state === 'recording') recorder.stop();
    });
})();
