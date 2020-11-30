(function (url) {
    function connect() {
        var socket = new WebSocket(url);
        socket.onmessage = function (event) {
            console.log("livex-onmessage:", event);
            if (event.data == "@") {
                window.location.reload();
            }
        }
        socket.onerror = function (event) {
            console.log("livex-onerror:", event);
            socket.close();
        }
        socket.onclose = function (event) {
            console.log("livex-onclose:", event);
            setTimeout(connect, 1000);
        }
    }
    connect();
})