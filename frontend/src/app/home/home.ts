import { Component, OnInit, inject, signal } from '@angular/core';
import { HealthService } from '../services/health.service';

type ApiState = 'checking' | 'online' | 'offline';

@Component({
  selector: 'app-home',
  styleUrl: './home.css',
  templateUrl: './home.html',
})
export class Home implements OnInit {
  private readonly health = inject(HealthService);

  protected readonly state = signal<ApiState>('checking');

  ngOnInit(): void {
    this.health.isOnline().subscribe((online) => this.state.set(online ? 'online' : 'offline'));
  }
}
