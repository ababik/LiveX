(function (url) {
    var body = document.querySelector("body");
    var styleLoader = null;
    var styleMessage = "@css:";
    function connect() {
        var socket = new WebSocket(url);
        socket.onmessage = function (event) {
            console.log("livex-onmessage:", event);
            if (event.data == "@") {
                window.location.reload();
            }
            if (event.data.startsWith(styleMessage)) {
                var url = event.data.substr(styleMessage.length);
                if (styleLoader == null) {
                    styleLoader = document.createElement("link");
                    styleLoader.setAttribute("rel", "stylesheet");
                    styleLoader.setAttribute("type", "text/css");
                    body.appendChild(styleLoader);
                }
                styleLoader.setAttribute("href", url + "?" + Date.now());
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