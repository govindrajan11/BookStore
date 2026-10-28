
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BookRepository, CatalogRepository } from '../domain/repositories';
import { AddBook, Book, CatalogBook, Genre, Page, UpdateBook } from '../domain/models';

const base = 'http://localhost:5080/api';

@Injectable()
export class HttpBookRepository extends BookRepository {
    private http = inject(HttpClient);

    list(
        genre: Genre | null,
        page: number,
        size: number,
        sort: string,
        desc: boolean
    ): Observable<Page<Book>> {
        let params = new HttpParams()
            .set('page', page)
            .set('pageSize', size)
            .set('sort', sort)
            .set('desc', desc);

        if (genre)
            params = params.set('genre', genre);

        return this.http.get<Page<Book>>(
            `${base}/books`,
            { params }
        );
    }

    add(book: AddBook) {
        return this.http.post<Book>(
            `${base}/books`,
            book
        );
    }

    update(id: string, book: UpdateBook) {
        return this.http.put<Book>(
            `${base}/books/${id}`,
            book
        );
    }

    delete(id: string) {
        return this.http.delete<void>(
            `${base}/books/${id}`
        );
    }
}

@Injectable()
export class HttpCatalogRepository extends CatalogRepository {
    private http = inject(HttpClient);

    search(query: string) {
        return this.http.get<CatalogBook[]>(
            `${base}/catalog/search`,
            { params: { q: query } }
        );
    }
}
