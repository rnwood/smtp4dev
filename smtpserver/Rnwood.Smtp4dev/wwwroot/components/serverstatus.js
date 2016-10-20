define(["text!./serverstatus.html", "api", "knockout"],
    function (template, api, ko) {
        function ServerStatusViewModel(options) {
            var self = this;

            var firstLoad = true;
            self.loading = ko.observable(true);
            self.error = ko.observable(null);
            self.isRunning = ko.observable(null);
            self.isAutoRefreshEnabled = ko.observable(false);

            self.refresh = function () {
                api.Server.find(0, { bypassCache: true }).then(function (data) {
                    self.loading(true);
                    self.error(data.error);
                    self.isRunning(data.isRunning);
                    self.loading(false);
                });
            }

            self.refresh();
        };

        return {
            viewModel: { viewModel: ServerStatusViewModel },
            template: template
        };
    }
);