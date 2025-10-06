import { Routes } from '@angular/router';
import { Healthcheck } from './healthcheck/healthcheck';
import { ProductList } from './product-list/product-list';

export const routes: Routes = [
  { path: 'healthcheck', component: Healthcheck },
  { path: 'products', component: ProductList }
];
