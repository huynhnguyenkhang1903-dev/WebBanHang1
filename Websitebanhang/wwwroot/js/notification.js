"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/notificationHub").build();

connection.on("ReceiveAdminNotification", function (message, url) {
    showToast(message, url, "linear-gradient(to right, #00b09b, #96c93d)");
});

connection.on("ReceiveUserNotification", function (message, url) {
    showToast(message, url, "linear-gradient(to right, #00b09b, #96c93d)");
});

connection.start().then(function () {
    console.log("SignalR Connected.");
}).catch(function (err) {
    return console.error(err.toString());
});

function showToast(message, url, background) {
    Toastify({
        text: message,
        duration: 5000,
        destination: url || "#",
        newWindow: false,
        close: true,
        gravity: "top", // `top` or `bottom`
        position: "right", // `left`, `center` or `right`
        stopOnFocus: true, // Prevents dismissing of toast on hover
        style: {
            background: background,
        },
        onClick: function(){} // Callback after click
    }).showToast();
}
