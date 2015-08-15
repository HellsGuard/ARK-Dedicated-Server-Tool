/// <reference path="../typings/jquery/jquery.d.ts"/>

module ArkBar
{
    'use strict'

    export function getMyIP(callback: (data: any) => void) {
        $.get("http://whatismyip.akamai.com/")
            .done(function (data: any) {
                callback(data);
            })
            .fail(function (error: any) {
                callback("Got an error: " + error);
            });
    };
    
    export class HostConnection {
        private hostAddress: string;
        private apiKey: string;

        constructor(addr: string, apiKey: string) {
            this.hostAddress = addr;
            this.apiKey = apiKey;
        }

        getServerStatus(callback: (data: any) => void) {
            $.get(this.hostAddress)
                .done(function (data: any) {
                    callback(data);
                })
                .fail(function (error: any) {
                    throw "Failed to get server status";
                });
        }
    }        
};