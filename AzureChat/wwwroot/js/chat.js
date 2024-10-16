"use strict";
var selectedUser = null;
document.addEventListener("DOMContentLoaded", function () {

    var user = localStorage.getItem("user");

    document.getElementById("nickname").innerHTML = "Nickname: " + user;

    if (!user) {
        alert("User is not logged in. Redirecting to login page.");
        window.location.href = "localhost:7062/";
        return;
    }

    var connection = new signalR.HubConnectionBuilder().withUrl("/chathub?username=" + encodeURIComponent(user)).build();

    connection.start().then(function () {
        console.log("Connected to SignalR as user:", user);
        connection.invoke("GetUserList").catch(function (err) {
            console.error("Error invoking GetUserList: ", err.toString());
        });

        connection.on("ReceiveMessage", function (senderNickname, message) {
            var messagesList = document.getElementById("messagesList");

            var li = document.createElement("li");
            li.textContent = `${senderNickname}: ${message}`;

            messagesList.appendChild(li);
        });


        connection.on("ReceivePublicMessage", function (senderNickname, message) {
            var publicMessagesList = document.getElementById("publicMessagesList");

            var li = document.createElement("li");
            li.textContent = `${senderNickname}: ${message}`;

            publicMessagesList.appendChild(li);
        });



        connection.on("ReceivePrivateMessage", function (senderNickname, message) {
            var messagesList = document.getElementById("messagesList");

            var li = document.createElement("li");
            li.textContent = `${senderNickname}: ${message}`;

            messagesList.appendChild(li);
        });


    }).catch(function (err) {
        console.error("Error connecting to SignalR: ", err.toString());
    });

    function sendMessageToAll(message) {
        if (!user) {
            console.error("User is not set, cannot send message.");
            return;
        }

        connection.invoke("SendMessage", user, message).then(function () {
            console.log(`Message sent to all: ${message}`);
            document.getElementById("messageInput").value = "";
        }).catch(function (err) {
            console.error("Error sending message to all: ", err.toString());
        });
    }

    connection.on("ReceiveUserList", function (users) {
        var usersList = document.getElementById("usersList");
        usersList.innerHTML = "";

        users.forEach(function (userObj) {
            var li = document.createElement("li");
            li.className = "list-group-item list-group-item-action";

            li.textContent = `${userObj.user.key}`;

            li.addEventListener("click", function () {
                selectedUser = userObj.user.key;
                console.log(`Opening private chat with ${selectedUser}`);

                document.getElementById("chatWindow").innerHTML = `<h5>Private chat with ${selectedUser}</h5>
                <ul id="messagesList" class="list-unstyled"></ul>`;

                connection.invoke("OpenPrivateChat", selectedUser).catch(function (err) {
                    return console.error(err.toString());
                });
            });

            usersList.appendChild(li);
        });
    });

    document.getElementById("generalChatButton").addEventListener("click", function () {
        connection.invoke("OpenGlobalChat").catch(function (err) {
            return console.error(err.toString());
        });
    });


    document.getElementById("sendButton").addEventListener("click", function (event) {
        var message = document.getElementById("messageInput").value;

        if (message) {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                console.error("Cannot send message because the connection is not in the 'Connected' state.");
                alert("Connection is not established. Please wait.");
                return;
            }

            if (selectedUser) {
                console.log(`Selected user: ${selectedUser}`);
                connection.invoke("SendPrivateMessage", user, selectedUser, message).then(function () {
                    console.log(`Private message sent to ${selectedUser}: ${message}`);
                    document.getElementById("messageInput").value = "";
                    var li = document.createElement("li");
                    li.textContent = `${user}: ${message}`;
                    var messagesList = document.getElementById("messagesList");
                    if (messagesList) {
                        messagesList.appendChild(li);
                    } else {
                        console.error("messagesList element not found!");
                    }
                }).catch(function (err) {
                    console.error("Error sending private message: ", err.toString());
                    alert("Failed to send message. Please try again.");
                });
            } else {
                sendMessageToAll(message);
            }
        } else {
            alert("Please enter a message.");
        }
        event.preventDefault();
    });

    document.getElementById("generalChatButton").addEventListener("click", function () {
        selectedUser = null;
        document.getElementById("chatWindow").innerHTML = `<h5>General Chat</h5>
             <ul id="messagesList" class="list-unstyled">
                        </ul>`;
    });

});

