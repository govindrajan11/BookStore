import { inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { AddBook, Book, CatalogBook, Genre, Page, UpdateBook } from '../domain/models';
import { BOOK_REPOSITORY, CATALOG_REPOSITORY } from '../infrastructure/tokens';

@Injectable({ providedIn: 'root' })
export class BookState {
    private booksRepo = inject(BOOK_REPOSITORY);
    private catalogRepo = inject(CATALOG_REPOSITORY);

    readonly genre = signal<Genre | null>(null);
    readonly page = signal(1);
    readonly size = signal(10);
    readonly sort = signal('title');
    readonly desc = signal(false);

    readonly books = signal<Book[]>([]);
    readonly total = signal(0);
    readonly loading = signal(false);

    readonly error = signal('');
    readonly saving = signal(false);

    readonly results = signal<CatalogBook[]>([]);
    readonly searching = signal(false);
    readonly searchError = signal('');

    private listVersion = 0;
    private searchVersion = 0;

    setGenre(genre: Genre | null) {
        this.genre.set(genre);
        this.page.set(1);
        this.load();
    }

    load() {
        const version = ++this.listVersion;

        this.loading.set(true);
        this.error.set('');

        this.booksRepo.list(
            this.genre(),
            this.page(),
            this.size(),
            this.sort(),
            this.desc()
        ).subscribe({
            next: p => {
                if (version === this.listVersion) {
                    this.books.set(p.items);
                    this.total.set(p.total);
                    this.loading.set(false);
                }
            },
            error: e => {
                if (version === this.listVersion) {
                    this.error.set(this.message(e));
                    this.loading.set(false);
                }
            }
        });
    }

    search(q: string) {
        if (q.trim().length < 2) {
            this.results.set([]);
            return;
        }

        const version = ++this.searchVersion;

        this.searching.set(true);
        this.searchError.set('');
        this.results.set([]);

        this.catalogRepo.search(q).subscribe({
            next: r => {
                if (version === this.searchVersion) {
                    this.results.set(r);
                    this.searching.set(false);
                }
            },
            error: e => {
                if (version === this.searchVersion) {
                    this.searchError.set(this.message(e));
                    this.searching.set(false);
                }
            }
        });
    }

    add(b: AddBook, done: () => void) {
        this.saving.set(true);
        this.error.set('');

        this.booksRepo.add(b)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe({
                next: () => {
                    this.load();
                    done();
                },
                error: e => this.error.set(this.message(e))
            });
    }

    update(id: string, b: UpdateBook, done: () => void) {
        this.saving.set(true);
        this.error.set('');

        this.booksRepo.update(id, b)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe({
                next: () => {
                    this.load();
                    done();
                },
                error: e => this.error.set(this.message(e))
            });
    }

    remove(id: string) {
        this.saving.set(true);
        this.error.set('');

        this.booksRepo.delete(id)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe({
                next: () => this.load(),
                error: e => this.error.set(this.message(e))
            });
    }

    private message(e: any) {
        return e?.error?.error || 'Request failed. Please try again.';
    }
}
