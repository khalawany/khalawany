(() => {
  const startBtn     = document.getElementById('startRecord');
  const stopBtn      = document.getElementById('stopRecord');
  const statusEl     = document.getElementById('recordStatus');
  const recDot       = document.getElementById('recDot');
  const mediaTypeSel = document.getElementById('mediaType');
  const previewVideo = document.getElementById('previewVideo');
  const noRecMsg     = document.getElementById('noRecorderMsg');

  if (!startBtn || !stopBtn) return;

  // Check MediaRecorder support
  if (typeof MediaRecorder === 'undefined' || !navigator.mediaDevices?.getUserMedia) {
    startBtn.disabled = true;
    stopBtn.disabled  = true;
    if (noRecMsg) noRecMsg.style.display = 'block';
    if (statusEl) statusEl.textContent = '';
    return;
  }

  // Pick best supported MIME type
  function getMimeType(isVideo) {
    const candidates = isVideo
      ? ['video/webm;codecs=vp9,opus', 'video/webm;codecs=vp8,opus', 'video/webm', 'video/mp4']
      : ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus', 'audio/mp4'];
    return candidates.find(t => MediaRecorder.isTypeSupported(t)) ?? '';
  }

  let mediaRecorder, chunks, stream;

  function setStatus(msg, dotState) {
    if (statusEl) statusEl.textContent = msg;
    if (recDot) {
      recDot.className = 'rec-dot' + (dotState ? ' ' + dotState : '');
    }
  }

  startBtn.addEventListener('click', async () => {
    try {
      const isVideo = mediaTypeSel?.value !== 'audio';
      stream = await navigator.mediaDevices.getUserMedia({ audio: true, video: isVideo });

      if (isVideo && previewVideo) {
        previewVideo.style.display = 'block';
        previewVideo.srcObject = stream;
      }

      const mimeType = getMimeType(isVideo);
      const options  = mimeType ? { mimeType } : {};
      mediaRecorder  = new MediaRecorder(stream, options);
      chunks         = [];

      mediaRecorder.ondataavailable = e => { if (e.data.size > 0) chunks.push(e.data); };

      mediaRecorder.onstop = () => {
        const blob = new Blob(chunks, { type: mediaRecorder.mimeType });
        const ext  = mediaRecorder.mimeType.includes('mp4') ? '.mp4'
                   : mediaRecorder.mimeType.includes('ogg') ? '.ogg'
                   : '.webm';
        const file = new File([blob], `recorded-${Date.now()}${ext}`, { type: mediaRecorder.mimeType });

        const dt = new DataTransfer();
        dt.items.add(file);
        const fileInput = document.getElementById('fileInput');
        if (fileInput) {
          fileInput.files = dt.files;
          const chosenFile = document.getElementById('chosenFile');
          if (chosenFile) chosenFile.textContent = file.name;
        }

        stream?.getTracks().forEach(t => t.stop());
        if (previewVideo) { previewVideo.srcObject = null; previewVideo.style.display = 'none'; }

        setStatus('Recording saved — ready to submit.', 'done');
        startBtn.disabled = false;
        stopBtn.disabled  = true;
      };

      mediaRecorder.start(250); // collect in 250ms chunks
      setStatus('Recording…', 'live');
      startBtn.disabled = true;
      stopBtn.disabled  = false;
    } catch (err) {
      const msg = err.name === 'NotAllowedError'
        ? 'Permission denied. Please allow camera/mic access and try again.'
        : 'Could not start recording: ' + err.message;
      setStatus(msg, '');
      startBtn.disabled = false;
    }
  });

  stopBtn.addEventListener('click', () => {
    if (mediaRecorder && mediaRecorder.state !== 'inactive') {
      mediaRecorder.stop();
    }
  });
})();
