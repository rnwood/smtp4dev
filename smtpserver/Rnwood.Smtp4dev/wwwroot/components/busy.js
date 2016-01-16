define(["text!./busy.html", "api", "knockout"],
    function (template, api, ko) {
        function BusyViewModel(options) {
            var self = this;

            self.busy = options.busy;
            self.message = options.message;
        };

        return {
            viewModel: { viewModel: BusyViewModel },
            template: template
        };
    }
);