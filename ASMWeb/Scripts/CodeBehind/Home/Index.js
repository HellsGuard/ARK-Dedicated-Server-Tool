/// <reference path="../../typings/jquery/jquery.d.ts"/>
/// <reference path="../../ASM/arkbar.ts"/>
function getApiKey() {
    return $('#apiKey').val();
}
function getApiHost() {
    return $('#apiHost').val();
}
function makeRequest() {
    var hostConnection = new ArkBar.HostConnection(getApiHost(), getApiKey());
    hostConnection.getServerStatus(writeOutput);
}
function writeOutput(output) {
    $('#output').text(output);
}
//# sourceMappingURL=Index.js.map