"use strict";
console.log("Chat.js loaded successfully");

var connection = null;
var user = '';
var password = '';
var userExists = false;

document.addEventListener("DOMContentLoaded", function () {

    document.getElementById("registerButton").addEventListener("click", function (event) {
        user = document.getElementById("userNickname").value;
        password = document.getElementById("userPassword").value;

        console.log(`Attempting to register user: ${user}, password: ${password}`);

        if (user && password) {
            userExists = false;

            connection = new signalR.HubConnectionBuilder().withUrl("/chathub?username=" + encodeURIComponent(user)).build();

            connection.start().then(function () {
                connection.invoke("RegisterUser", user, password).then(function () {
                    if (!userExists) {
                        console.log("User registered successfully on server.");
                        localStorage.setItem("user", user);
                        window.location.href = "/chat"; 
                    }
                }).catch(function (err) {
                    console.error("Error invoking RegisterUser: ", err.toString());
                });
            }).catch(function (err) {
                console.error("Error starting SignalR connection: ", err.toString());
            });

        } else {
            alert("Please enter both username and password.");
        }
        event.preventDefault();
    });

    document.getElementById("loginButton").addEventListener("click", function (event) {
        user = document.getElementById("userNickname").value;
        password = document.getElementById("userPassword").value;

        if (user && password) {
            connection = new signalR.HubConnectionBuilder().withUrl("/chathub?username=" + encodeURIComponent(user)).build();

            connection.start().then(function () {
                connection.invoke("Login", user, password).then(function () {
                    console.log("Attempting to login...");
                    localStorage.setItem("user", user);
                    window.location.href = "/chat"; 
                }).catch(function (err) {
                    console.error("Error invoking Login: ", err.toString());
                });
            }).catch(function (err) {
                console.error("Error starting SignalR connection: ", err.toString());
            });
        } else {
            alert("Please enter both username and password.");
        }
        event.preventDefault();
    });

    if (connection) {
        connection.on("UserAlreadyExists", function (message) {
            alert(message);
            userExists = true;
        });

        connection.on("LoginSuccessful", function (userId) {
            console.log("User logged in successfully on server.");
            window.location.href = "/chat"; 
        });

        connection.on("LoginFailed", function (errorMessage) {
            alert(errorMessage);
        });
    }
});
