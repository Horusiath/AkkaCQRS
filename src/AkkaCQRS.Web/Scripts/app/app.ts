/// <reference path="../jspm_packages/github/aurelia/route-recognizer@0.6.1/aurelia-route-recognizer.d.ts" />
/// <reference path="../jspm_packages/github/aurelia/router@0.10.3/aurelia-router.d.ts" />

import {RouterConfiguration, Router} from 'aurelia-router';

export class App {
    router: Router;
    configureRouter(config: RouterConfiguration, router: Router) {
        config.title = 'Akka.NET sample';

        config.map([
          { route: ['','welcome'], name: 'welcome', moduleId: './welcome', nav: true, title: 'Welcome' },
          { route: ['','auth'], name: 'auth', moduleId: './auth/auth', nav: true, title: 'Register' },
          { route: '', redirect: 'auth' }
        ]);

        this.router = router;
    }
}