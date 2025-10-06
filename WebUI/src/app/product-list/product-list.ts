import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment.development';

export interface Product {
  productId: number;
  articleNumber: string;
  articleName: string;
  description: string;
  category: string;
  tags: string[];
}

@Component({
  selector: 'app-product-list',
  imports: [NgbModule],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css'
})
export class ProductList implements OnInit {
  private http = inject(HttpClient);
  public products = signal<Product[]>([]);

  async ngOnInit(): Promise<void> {
    const result = await firstValueFrom(this.http.get<Product[]>(
      `${environment.apiBaseUrl}/products`));
    this.products.set(result);
  }
}
