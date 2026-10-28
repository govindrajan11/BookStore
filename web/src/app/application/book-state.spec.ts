import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { BookState } from './book-state';
import { BOOK_REPOSITORY, CATALOG_REPOSITORY } from '../infrastructure/tokens';
import type { Book, Page, CatalogBook } from '../domain/models';

const fantasyBook: Book = {
  id: '1', openLibraryKey: '/works/OL1W', title: 'The Hobbit', author: 'Tolkien',
  isbn: null, publishedYear: 1937, coverUrl: null, description: null,
  genre: 'Fantasy', price: 15, stock: 6
};

function setup() {
  const booksRepo = {
    list: vi.fn().mockReturnValue(of({ items: [fantasyBook], total: 1, page: 1, pageSize: 10 } as Page<Book>)),
    add: vi.fn().mockReturnValue(of(fantasyBook)),
    update: vi.fn().mockReturnValue(of(fantasyBook)),
    delete: vi.fn().mockReturnValue(of(void 0))
  };
  const catalogRepo = { search: vi.fn().mockReturnValue(of([] as CatalogBook[])) };
  TestBed.configureTestingModule({ providers: [
    BookState,
    { provide: BOOK_REPOSITORY, useValue: booksRepo },
    { provide: CATALOG_REPOSITORY, useValue: catalogRepo }
  ] });
  return { state: TestBed.inject(BookState), booksRepo, catalogRepo };
}

describe('BookState', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('loads books from the repository and updates signals', () => {
    const { state, booksRepo } = setup();
    state.load();
    expect(booksRepo.list).toHaveBeenCalledWith(null, 1, 10, 'title', false);
    expect(state.books()).toEqual([fantasyBook]);
    expect(state.total()).toBe(1);
    expect(state.loading()).toBe(false);
  });

  it('shares genre selection and resets pagination', () => {
    const { state, booksRepo } = setup();
    state.page.set(3);
    state.setGenre('Fantasy');
    expect(state.genre()).toBe('Fantasy');
    expect(state.page()).toBe(1);
    expect(booksRepo.list).toHaveBeenCalledWith('Fantasy', 1, 10, 'title', false);
  });

  it('shows an error when listing fails', () => {
    const { state, booksRepo } = setup();
    booksRepo.list.mockReturnValue(throwError(() => ({ error: { error: 'API unavailable' } })));
    state.load();
    expect(state.error()).toBe('API unavailable');
    expect(state.loading()).toBe(false);
  });

  it('ignores search terms shorter than two characters', () => {
    const { state, catalogRepo } = setup();
    state.search('a');
    expect(catalogRepo.search).not.toHaveBeenCalled();
    expect(state.results()).toEqual([]);
  });

  it('stores catalog search results', () => {
    const { state, catalogRepo } = setup();
    const result: CatalogBook = { openLibraryKey: '/works/OL1W', title: 'The Hobbit', author: 'Tolkien', isbn: null, publishedYear: 1937, coverUrl: null };
    catalogRepo.search.mockReturnValue(of([result]));
    state.search('hobbit');
    expect(catalogRepo.search).toHaveBeenCalledWith('hobbit');
    expect(state.results()).toEqual([result]);
    expect(state.searching()).toBe(false);
  });

  it('reloads books and calls completion callback after add', () => {
    const { state, booksRepo } = setup();
    const done = vi.fn();
    state.add({ ...fantasyBook }, done);
    expect(booksRepo.add).toHaveBeenCalledOnce();
    expect(booksRepo.list).toHaveBeenCalledOnce();
    expect(done).toHaveBeenCalledOnce();
    expect(state.saving()).toBe(false);
  });
});
