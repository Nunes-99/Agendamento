import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/theme.service';
import { TenantService } from './core/services/tenant.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet></router-outlet>`,
  styles: [':host { display: block; min-height: 100vh; }']
})
export class AppComponent implements OnInit {
  private theme = inject(ThemeService);
  private tenant = inject(TenantService);

  ngOnInit(): void {
    this.tenant.detectarSlug();
    this.theme.aplicarPadrao();
    if (this.tenant.slug) {
      this.tenant.carregarTenant(this.tenant.slug).subscribe(t => {
        if (t) this.theme.aplicarPersonalizacao(t.personalizacao);
      });
    }
  }
}
