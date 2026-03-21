let html5QrCode = null;

export async function startScanner(containerId, dotNetRef) {
    html5QrCode = new Html5Qrcode(containerId);

    try {
        await html5QrCode.start(
            { facingMode: "environment" },
            {
                fps: 8,
                qrbox: 200,
                aspectRatio: 1.0,
                videoConstraints: {
                    facingMode: "environment",
                    width: { ideal: 640 },
                    height: { ideal: 640 }
                },
                disableFlip: true,
                useBarCodeDetectorIfSupported: false
            },
            (decodedText, decodedResult) => {
                const code = decodedText.trim().toUpperCase().slice(0, 6);
                if (code.length >= 4) {
                    dotNetRef.invokeMethodAsync("OnCodeScanned", code);
                }
            },
            (err) => { console.log("Scan err:", err); }
        );

        await new Promise(r => setTimeout(r, 800));
        const video = document.querySelector(`#${containerId} video`);
        if (video) {
            video.setAttribute('playsinline', '');
            video.setAttribute('webkit-playsinline', '');
            video.muted = true;
            console.log("playsinline forced on internal video");
        }
    } catch (err) {
        console.error("Start failed:", err);
        dotNetRef.invokeMethodAsync("OnScanError", err + "");
    }
}

export function stopScanner() {
    if (html5QrCode) {
        html5QrCode.stop().catch(() => {});
        html5QrCode = null;
    }
}