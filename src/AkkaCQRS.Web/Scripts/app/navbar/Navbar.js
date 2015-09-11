var samples;
(function (samples) {
    var navbar;
    (function (navbar) {
        navbar.Section = React.createClass({
            render: function () {
                var wrapped = React.Children.map(this.props.children, function (child) { return React.createElement("li", null, child); });
                var className = "nav navbar-nav navbar-" + this.props.direction;
                return (React.createElement("ul", {"className": className}, wrapped));
            }
        });
        navbar.Navbar = React.createClass({
            render: function () {
                return (React.createElement("nav", {"className": "navbar navbar-fixed-top navbar-inverse"}, React.createElement("div", {"className": "container-fluid"}, React.createElement("div", {"className": "navbar-header"}, React.createElement("a", {"className": "navbar-brand", "href": "#"}, this.props.appTitle)), this.props.children)));
            }
        });
    })(navbar = samples.navbar || (samples.navbar = {}));
})(samples || (samples = {}));
//# sourceMappingURL=navbar.js.map