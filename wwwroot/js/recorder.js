(() => {
  const startBtn = document.getElementById('startRecord');
  const stopBtn = document.getElementById('stopRecord');
  const status = document.getElementById('recordStatus');
  const mediaTypeSelect = document.getElementById('mediaType');
  const previewVideo = document.getElementById('previewVideo');

  if (!startBtn || !stopBtn) return;

  let mediaRecorder;
  let chunks = [];
  let stream;

  startBtn.addEventListener('click', async () => {
    try {
      const isVideo = mediaTypeSelect?.value !== 'audio';
      stream = await navigator.mediaDevices.getUserMedia({
        audio: true,
        video: isVideo
      });

      if (isVideo && previewVideo) {
        previewVideo.style.display = 'block';
        previewVideo.srcObject = stream;
      }

      const mimeType = isVideo ? 'video/webm' : 'audio/webm';
      mediaRecorder = new MediaRecorder(stream, { mimeType });
      chunks = [];

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) chunks.push(event.data);
      };

      mediaRecorder.onstop = () => {
        const blob = new Blob(chunks, { type: mimeType });
        const file = new File([blob], `recorded-${Date.now()}.webm`, { type: mimeType });

        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);

        const input = document.getElementById('fileInput');
        if (input) input.files = dataTransfer.files;

        status.textContent = 'Recorded and attached to upload form.';

        stream?.getTracks().forEach(t => t.stop());
        if (previewVideo) previewVideo.srcObject = null;
      };

      mediaRecorder.start();
      status.textContent = 'Recording...';
    } catch (err) {
      status.textContent = 'Unable to start recording. Check browser permissions.';
    }
  });

  stopBtn.addEventListener('click', () => {
    if (mediaRecorder && mediaRecorder.state !== 'inactive') {
      mediaRecorder.stop();
    }
  });
})();
