import { Component } from '@angular/core';
import { HeaderComponent } from './presentation/header';
import { BodyComponent } from './presentation/body';
import { FooterComponent } from './presentation/footer';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [
        HeaderComponent,
        BodyComponent,
        FooterComponent
    ],
    template: `
        <app-header/>
        <app-body/>
        <app-footer/>
    `
})
export class AppComponent {
}
