namespace samples.navbar {

    export interface INavbarSectionProps extends React.Props<{}> {
        direction?: string
    }

    export var Section = React.createClass<INavbarSectionProps, {}>({
        render: function () {
            var wrapped = React.Children.map(this.props.children, (child: React.ReactChild) => <li>{child}</li>);
            var className = "nav navbar-nav navbar-" + this.props.direction;
            return (
                <ul className={className}>
                    {wrapped}
                </ul>);
        }
    });

    export interface INavbarProps extends React.Props<{}> {
        appTitle: string
    }
    export var Navbar = React.createClass<INavbarProps, {}>({
        render: function () {
            return (
                <nav className="navbar navbar-fixed-top navbar-inverse">
                  <div className="container-fluid">
                    <div className="navbar-header">
                      <a className="navbar-brand" href="#">{this.props.appTitle}</a>
                    </div>

                    {this.props.children}
                  </div>
                </nav>
            );
        }
    });

}