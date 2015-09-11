/// <reference path="app/navbar/navbar.tsx" />

import nav = samples.navbar;

React.render(
    <nav.Navbar appTitle="Akka CQRS Sample">
        <nav.Section>
            <a href="#">Home</a>
        </nav.Section>
        <nav.Section direction="right">
            <a href="#">Sign up</a>
        </nav.Section>
    </nav.Navbar>,
    document.getElementById('content'));
