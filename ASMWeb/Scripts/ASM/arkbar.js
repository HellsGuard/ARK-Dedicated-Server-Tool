/// <reference path="../typings/jquery/jquery.d.ts"/>
var ArkBar;
(function (ArkBar) {
    'use strict';
    function getMyIP(callback) {
        $.get("http://whatismyip.akamai.com/")
            .done(function (data) {
            callback(data);
        })
            .fail(function (error) {
            callback("Got an error: " + error);
        });
    }
    ArkBar.getMyIP = getMyIP;
    ;
    var HostConnection = (function () {
        function HostConnection(addr, apiKey) {
            this.hostAddress = addr;
            this.apiKey = apiKey;
        }
        HostConnection.prototype.getServerStatus = function (callback) {
            $.get(this.hostAddress)
                .done(function (data) {
                callback(data);
            })
                .fail(function (error) {
                throw "Failed to get server status";
            });
        };
        return HostConnection;
    })();
    ArkBar.HostConnection = HostConnection;
})(ArkBar || (ArkBar = {}));
;
