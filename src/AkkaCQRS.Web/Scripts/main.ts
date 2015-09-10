export function configure(aurelia) {
    if (!!aurelia) {
        aurelia.use
            .standardConfiguration()
            .developmentLogging();

        aurelia.start().then(a => a.setRoot('app/app', document.body));
    }
}