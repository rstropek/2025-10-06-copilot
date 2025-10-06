import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment.development';

@Component({
  selector: 'app-healthcheck',
  imports: [NgbModule],
  templateUrl: './healthcheck.html',
  styleUrl: './healthcheck.css'
})
export class Healthcheck implements OnInit {
  private http = inject(HttpClient);
  public healthy = signal<boolean | undefined>(undefined);

  async ngOnInit(): Promise<void> {
    const result = await firstValueFrom(this.http.get<{ healthy: boolean }>(
      `${environment.apiBaseUrl}/health`));
    this.healthy.set(result.healthy);
  }
}
