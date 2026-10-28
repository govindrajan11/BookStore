import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { BookState } from '../application/book-state';
import { AddBook, Book, CatalogBook, Genre, genres } from '../domain/models';

@Component({
    selector: 'app-body',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        InputNumberModule,
        SelectModule
    ],
    template: `
        <main class="max-w-6xl mx-auto px-4 py-8 min-h-[65vh]">

            <div class="flex items-center justify-between mb-5">
                <div>
                    <h2 class="text-xl font-semibold">Store inventory</h2>
                    <p class="text-slate-500">{{state.total()}} saved books</p>
                </div>

                <p-button
                    label="Add book"
                    icon="pi pi-plus"
                    (onClick)="openAdd()"
                />
            </div>

            <p
                *ngIf="state.error()"
                role="alert"
                class="text-red-700 bg-red-50 p-3 rounded mb-4"
            >
                {{state.error()}}
            </p>

            <p-table
                [value]="state.books()"
                [lazy]="true"
                [loading]="state.loading()"
                [paginator]="true"
                [rows]="state.size()"
                [totalRecords]="state.total()"
                [first]="(state.page()-1)*state.size()"
                (onLazyLoad)="tableChanged($event)"
                [rowsPerPageOptions]="[5,10,20]"
                responsiveLayout="scroll"
            >
                <ng-template #header>
                    <tr>
                        <th>Cover</th>

                        <th pSortableColumn="title">
                            Title
                            <p-sortIcon field="title" />
                        </th>

                        <th pSortableColumn="author">
                            Author
                            <p-sortIcon field="author" />
                        </th>

                        <th>Genre</th>

                        <th pSortableColumn="price">
                            Price
                            <p-sortIcon field="price" />
                        </th>

                        <th pSortableColumn="stock">
                            Stock
                            <p-sortIcon field="stock" />
                        </th>

                        <th>Actions</th>
                    </tr>
                </ng-template>

                <ng-template #body let-b>
                    <tr>
                        <td>
                            <img
                                *ngIf="b.coverUrl"
                                [src]="b.coverUrl"
                                alt="Book cover"
                                class="w-12 h-16 object-cover"
                            />
                        </td>

                        <td>{{b.title}}</td>
                        <td>{{b.author}}</td>
                        <td>{{b.genre}}</td>
                        <td>{{b.price | currency}}</td>
                        <td>{{b.stock}}</td>

                        <td>
                            <div class="flex gap-2">
                                <p-button
                                    label="Edit"
                                    severity="secondary"
                                    size="small"
                                    (onClick)="openEdit(b)"
                                />

                                <p-button
                                    label="Delete"
                                    severity="danger"
                                    size="small"
                                    [disabled]="state.saving()"
                                    (onClick)="remove(b)"
                                />
                            </div>
                        </td>
                    </tr>
                </ng-template>

                <ng-template #emptymessage>
                    <tr>
                        <td colspan="7" class="text-center py-10">
                            No books found for this filter.
                        </td>
                    </tr>
                </ng-template>
            </p-table>

            <p-dialog
                header="Add a book"
                [(visible)]="adding"
                [modal]="true"
                [style]="{width:'min(92vw,650px)'}"
            >
                <div *ngIf="!selected" class="space-y-4">
                    <label class="block">
                        Search Open Library by title, author, or ISBN
                    </label>

                    <div class="flex gap-2">
                        <input
                            pInputText
                            class="flex-1 min-w-0"
                            [(ngModel)]="query"
                            (keyup.enter)="search()"
                            placeholder="e.g. Dune"
                        />

                        <p-button
                            label="Search"
                            [loading]="state.searching()"
                            (onClick)="search()"
                        />
                    </div>

                    <p
                        *ngIf="state.searchError()"
                        class="text-red-700"
                    >
                        {{state.searchError()}}
                    </p>

                    <p *ngIf="!state.searching() && searched && !state.results().length && !state.searchError()">
                        No results found.
                    </p>

                    <div
                        *ngFor="let result of state.results()"
                        class="flex items-center gap-3 border-b py-3"
                    >
                        <img
                            *ngIf="result.coverUrl"
                            [src]="result.coverUrl"
                            alt="Cover"
                            class="w-10 h-14 object-cover"
                        />

                        <div class="flex-1">
                            <div class="font-semibold">
                                {{result.title}}
                            </div>

                            <div class="text-sm text-slate-500">
                                {{result.author}} · {{result.publishedYear || 'Year unknown'}}
                            </div>
                        </div>

                        <p-button
                            label="Select"
                            size="small"
                            (onClick)="choose(result)"
                        />
                    </div>
                </div>

                <div *ngIf="selected" class="space-y-4">
                    <p class="font-semibold">
                        {{selected.title}} — {{selected.author}}
                    </p>

                    <ng-container *ngTemplateOutlet="fields"/>

                    <div class="flex gap-2">
                        <p-button
                            label="Back"
                            severity="secondary"
                            (onClick)="selected=null"
                        />

                        <p-button
                            label="Save book"
                            [disabled]="!valid()"
                            [loading]="state.saving()"
                            (onClick)="saveAdd()"
                        />
                    </div>
                </div>
            </p-dialog>

            <p-dialog
                header="Edit book"
                [(visible)]="editing"
                [modal]="true"
                [style]="{width:'min(92vw,500px)'}"
            >
                <div *ngIf="editBook" class="space-y-4">
                    <p class="font-semibold">
                        {{editBook.title}}
                    </p>

                    <ng-container *ngTemplateOutlet="fields"/>

                    <p-button
                        label="Save changes"
                        [disabled]="!valid()"
                        [loading]="state.saving()"
                        (onClick)="saveEdit()"
                    />
                </div>
            </p-dialog>

            <ng-template #fields>
                <div>
                    <label class="block mb-1">Genre</label>

                    <p-select
                        [options]="genreOptions"
                        [(ngModel)]="genre"
                        class="w-full"
                    />
                </div>

                <div>
                    <label class="block mb-1">Price</label>

                    <p-inputnumber
                        [(ngModel)]="price"
                        mode="currency"
                        currency="USD"
                        [min]="0"
                        [max]="1000000"
                    />
                </div>

                <div>
                    <label class="block mb-1">Stock</label>

                    <p-inputnumber
                        [(ngModel)]="stock"
                        [min]="0"
                        [maxFractionDigits]="0"
                    />
                </div>

                <div>
                    <label class="block mb-1">Description (optional)</label>

                    <input
                        pInputText
                        [(ngModel)]="description"
                        class="w-full"
                    />
                </div>

                <p
                    *ngIf="state.error()"
                    role="alert"
                    class="text-red-700"
                >
                    {{state.error()}}
                </p>
            </ng-template>
        </main>
    `
})
export class BodyComponent implements OnInit {
    state = inject(BookState);

    adding = false;
    editing = false;

    selected: CatalogBook | null = null;
    editBook: Book | null = null;

    query = '';
    searched = false;

    genreOptions = [...genres];
    genre: Genre = 'Fiction';

    price: number | null = 0;
    stock: number | null = 0;
    description = '';

    ngOnInit() {
        this.state.load();
    }

    tableChanged(e: TableLazyLoadEvent) {
        this.state.page.set(
            Math.floor((e.first ?? 0) / (e.rows ?? 10)) + 1
        );

        this.state.size.set(e.rows ?? 10);

        this.state.sort.set(
            typeof e.sortField === 'string' ? e.sortField : 'title'
        );

        this.state.desc.set(e.sortOrder === -1);
        this.state.load();
    }

    openAdd() {
        this.state.error.set('');
        this.adding = true;
        this.selected = null;
        this.query = '';
        this.searched = false;
        this.state.results.set([]);
        this.genre = 'Fiction';
        this.price = 0;
        this.stock = 0;
        this.description = '';
    }

    search() {
        this.searched = true;
        this.state.search(this.query);
    }

    choose(b: CatalogBook) {
        this.selected = b;
        this.state.error.set('');
    }

    openEdit(b: Book) {
        this.state.error.set('');
        this.editBook = b;
        this.genre = b.genre;
        this.price = b.price;
        this.stock = b.stock;
        this.description = b.description ?? '';
        this.editing = true;
    }

    valid() {
        return this.price !== null &&
            Number.isFinite(this.price) &&
            this.price >= 0 &&
            this.price <= 1000000 &&
            this.stock !== null &&
            Number.isInteger(this.stock) &&
            this.stock >= 0;
    }

    saveAdd() {
        if (!this.selected || !this.valid()) return;

        this.state.add(
            {
                ...this.selected,
                genre: this.genre,
                price: this.price!,
                stock: this.stock!,
                description: this.description
            },
            () => {
                this.adding = false;
            }
        );
    }

    saveEdit() {
        if (!this.editBook || !this.valid()) return;

        this.state.update(
            this.editBook.id,
            {
                genre: this.genre,
                price: this.price!,
                stock: this.stock!,
                description: this.description
            },
            () => {
                this.editing = false;
            }
        );
    }

    remove(b: Book) {
        if (confirm(`Delete ${b.title}?`))
            this.state.remove(b.id);
    }
}
