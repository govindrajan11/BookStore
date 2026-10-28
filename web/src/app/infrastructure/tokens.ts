import {InjectionToken} from '@angular/core';
import {BookRepository,CatalogRepository} from '../domain/repositories';
export const BOOK_REPOSITORY=new InjectionToken<BookRepository>('BOOK_REPOSITORY');
export const CATALOG_REPOSITORY=new InjectionToken<CatalogRepository>('CATALOG_REPOSITORY');
