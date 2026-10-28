export const genres = ['Fiction','SciFi','Fantasy','Mystery','Biography','NonFiction','Children','History'] as const;
export type Genre = typeof genres[number];
export interface CatalogBook {openLibraryKey:string;title:string;author:string;isbn:string|null;publishedYear:number|null;coverUrl:string|null;}
export interface Book extends CatalogBook {id:string;genre:Genre;price:number;stock:number;description:string|null;}
export interface Page<T> {items:T[];total:number;page:number;pageSize:number;}
export interface AddBook extends CatalogBook {genre:Genre;price:number;stock:number;description:string|null;}
export interface UpdateBook {genre:Genre;price:number;stock:number;description:string|null;}
