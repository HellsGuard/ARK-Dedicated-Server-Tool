/// <reference path="../../typings/jquery/jquery.d.ts"/>
/// <reference path="../../ASM/arkbar.ts"/>

function getApiKey(): string {
    return $('#apiKey').val();
}

function getApiHost(): string {
    return $('#apiHost').val();
}

function makeRequest() {
    var hostConnection = new ArkBar.HostConnection(getApiHost(), getApiKey());
    hostConnection.getServerStatus(writeOutput);
}

function writeOutput(output: string) {
    $('#output').text(output);
}