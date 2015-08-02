export class App {
    configureRouter(config, router) {
        config.title = 'Akka.NET sample';

        config.map([
          { route: ['','welcome'], name: 'welcome', moduleId: './welcome', nav: true, title: 'Welcome' },
          { route: ['','signin'], name: 'signin', moduleId: './signin', nav: true, title: 'Sign in' },
          { route: '', redirect: 'signin' }
        ]);

        this.router = router;
    }
}