define(["knockout", "api", "toastr", "text!./settingsview.html"],
    function (ko, api, toastr, template) {
        function SettingsViewModel(params) {
            var self = this;

            self.loading = ko.observable(false);
            self.saving = ko.observable(false);
            self.port = ko.observable();
            self.isEnabled = ko.observable();

            var data = null;

            self.refresh = function () {
                self.loading(true);

                api.Server.find(0).then(function (server) {
                    data = server;
                    self.port(data.port);
                    self.isEnabled(data.isEnabled);
                    self.loading(false);
                });
            };

            self.save = function () {
                self.saving(true);

                data.isEnabled = self.isEnabled();
                data.port = self.port();

                api.Server.save(data.id).then(function () {
                    self.saving(false);
                    toastr.success("Settings saved");
                });
            };

            self.refresh();
        }

        return {
            viewModel: { viewModel: SettingsViewModel },
            template: template
        };
    }
);