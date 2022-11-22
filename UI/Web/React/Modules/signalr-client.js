import $ from 'jquery';

// hacky jQuery imports because we're using the old .NET hub and not .NET Core!
const windowJquery = window.$;
window.$ = window.jQuery = require("jquery");
require('signalr');
window.$ = window.jQuery = windowJquery;

export class SignalRManager {
    constructor(appPath, currentUserId) {
        this.appPath = appPath;
        this.currentUserId = currentUserId;
    }

    getHub() {
        return $.hubConnection(`${this.appPath}signalr`, { useDefaultPath: false });
    }

    registerMessageListeners(listeningThreadId, onNewMessage, onReactionsChanged, onSyncRequired) {
        const hub = this.getHub();

        hub.reconnected(() => onSyncRequired());
        hub.stateChanged((change) => {
            if (change.newState === 4) { // disconnected
                if (change.oldState === 0) { // connecting
                    // connection failed.  Request a sync to check/report connectivity issue...
                    onSyncRequired();
                }

                // allow a second for any network issues then reconnect
                setTimeout(() => hub.start(), 1000);
            }
        });

        const hubProxy = hub.createHubProxy("messages");

        hubProxy.on('newMessage', (userId, threadId) => {
            if (userId !== this.currentUserId && threadId === listeningThreadId) {
                onNewMessage();
            }
        });

        hubProxy.on('reactionsChanged', (userId, threadId, messageId) => {
            if (userId !== this.currentUserId && threadId === listeningThreadId) {
                onReactionsChanged(messageId);
            }
        });
        
        hub.start();
    }
}
