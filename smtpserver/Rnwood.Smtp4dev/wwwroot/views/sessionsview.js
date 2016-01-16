define(["text!./sessionsview.html"],
    function (template) {
        function SessionsViewModel(params) {
            var self = this;
        }

        return {
            viewModel: { viewModel: SessionsViewModel },
            template: template
        };
    }
);