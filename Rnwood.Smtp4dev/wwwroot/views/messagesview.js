define(["text!./messagesview.html"],
    function (template) {
        function MessagesViewModel(params) {
            var self = this;
        }

        return {
            viewModel: { viewModel: MessagesViewModel },
            template: template
        };
    }
);