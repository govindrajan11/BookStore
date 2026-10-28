import {AddBook,Book,CatalogBook,Genre,Page,UpdateBook} from './models';
import {Observable} from 'rxjs';
export abstract class BookRepository {
 abstract list(genre:Genre|null,page:number,size:number,sort:string,desc:boolean):Observable<Page<Book>>;
 abstract add(book:AddBook):Observable<Book>;
 abstract update(id:string,book:UpdateBook):Observable<Book>;
 abstract delete(id:string):Observable<void>;
}
export abstract class CatalogRepository { abstract search(query:string):Observable<CatalogBook[]>; }
