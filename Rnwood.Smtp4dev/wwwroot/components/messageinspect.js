define(["text!./messageinspect.html", "api", "knockout", "moment"],
    function (template, api, ko, moment) {
        function MessageInspectViewModel(options) {
            var self = this;
        };

        return {
            viewModel: { viewModel: MessageInspectViewModel },
            template: template
        };
    }
);