import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  imports: [NgbModule, FormsModule],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css'
})
export class ProductList implements OnInit {
  private http = inject(HttpClient);
  public products = signal<Product[]>([]);
  public categories = signal<string[]>([]);
  public selectedCategory = signal<string>('');
  public searchText = signal<string>('');

  async ngOnInit(): Promise<void> {
    await this.loadCategories();
    await this.loadProducts();
  }

  private async loadCategories(): Promise<void> {
    const result = await firstValueFrom(this.http.get<string[]>(
      `${environment.apiBaseUrl}/products/categories`));
    this.categories.set(result);
  }

  private async loadProducts(category?: string, text?: string): Promise<void> {
    let params = new HttpParams();
    if (category) {
      params = params.set('category', category);
    }
    if (text) {
      params = params.set('text', text);
    }

    const result = await firstValueFrom(this.http.get<Product[]>(
      `${environment.apiBaseUrl}/products`, { params }));
    this.products.set(result);
  }

  async applyFilter(): Promise<void> {
    const category = this.selectedCategory();
    const text = this.searchText();
    await this.loadProducts(category || undefined, text || undefined);
  }
}
