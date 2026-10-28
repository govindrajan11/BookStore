
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { BookState } from '../application/book-state';
import { genres, Genre } from '../domain/models';

@Component({
    selector: 'app-header',
    standalone: true,
    imports: [
        FormsModule,
        SelectModule
    ],
    template: `
        <header class="bg-slate-900 text-white px-6 py-5 flex flex-wrap items-center justify-between gap-4">
            <div>
                <h1 class="text-2xl font-bold">Book Store</h1>
                <p class="text-sm text-slate-300">
                    Curated catalog, owned inventory
                </p>
            </div>

            <div class="flex items-center gap-3">
                <label for="genre">Filter by genre</label>

                <p-select
                    inputId="genre"
                    [options]="options"
                    optionLabel="label"
                    optionValue="value"
                    [ngModel]="state.genre()"
                    (ngModelChange)="state.setGenre($event)"
                    class="w-48"
                />
            </div>
        </header>
    `
})
export class HeaderComponent {
    state = inject(BookState);

    options = [
        { label: 'All genres', value: null },
        ...genres.map(g => ({
            label: g,
            value: g as Genre
        }))
    ];
}
