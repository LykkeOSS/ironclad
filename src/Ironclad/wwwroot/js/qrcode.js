new QRCode(document.getElementById("qrCode"),
    {
        text: document.getElementById("qrCode").getAttribute("data-text"),
        width: 150,
        height: 150
    });