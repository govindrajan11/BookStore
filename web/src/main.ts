
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';

import { AppComponent } from './app/app';
import { BOOK_REPOSITORY, CATALOG_REPOSITORY } from './app/infrastructure/tokens';
import { HttpBookRepository, HttpCatalogRepository } from './app/infrastructure/http-repositories';

bootstrapApplication(AppComponent, {
    providers: [
        provideHttpClient(),

        providePrimeNG({
            theme: {
                preset: Aura
            }
        }),

        {
            provide: BOOK_REPOSITORY,
            useClass: HttpBookRepository
        },

        {
            provide: CATALOG_REPOSITORY,
            useClass: HttpCatalogRepository
        }
    ]
}).catch(console.error);
