define(["knockout", "api", "toastr", "text!./viewmessageview.html"],
    function (ko, api, toastr, template) {
        function ViewMessageViewModel(params) {
            var self = this;

            self.loading = ko.observable(false);
            self.id = ko.observable();

            self.refresh = function () {
                self.loading(true);

                self.id = params.id;

                self.loading(false);
            };

            self.refresh();
        }

        return {
            viewModel: { viewModel: ViewMessageViewModel },
            template: template
        };
    }
);