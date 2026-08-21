import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Plano } from '../planos/plano';
import { PlanoServico } from '../planos/plano-servico';
import { Beneficiario, StatusBeneficiario } from './beneficiario';
import { BeneficiarioServico } from './beneficiario-servico';
import { BeneficiarioForm } from './beneficiario-form';

@Component({
  selector: 'app-beneficiarios-lista',
  imports: [FormsModule, BeneficiarioForm],
  templateUrl: './beneficiarios-lista.html',
  styleUrl: './beneficiarios-lista.css',
})
export class BeneficiariosLista implements OnInit {
  private readonly servico = inject(BeneficiarioServico);
  private readonly planoServico = inject(PlanoServico);

  readonly beneficiarios = signal<Beneficiario[]>([]);
  readonly planos = signal<Plano[]>([]);
  readonly total = signal(0);
  readonly carregando = signal(false);
  readonly erro = signal<string | null>(null);
  readonly mostrandoFormulario = signal(false);
  readonly beneficiarioEmEdicao = signal<Beneficiario | null>(null);

  pagina = 1;
  readonly tamanho = 10;
  filtroStatus: StatusBeneficiario | '' = '';
  filtroPlanoId = '';

  ngOnInit(): void {
    this.planoServico.listar().subscribe({
      next: (planos) => this.planos.set(planos),
    });
    this.carregar();
  }

  carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.servico
      .listar({
        pagina: this.pagina,
        tamanho: this.tamanho,
        status: this.filtroStatus || null,
        plano_id: this.filtroPlanoId || null,
      })
      .subscribe({
        next: (resposta) => {
          this.beneficiarios.set(resposta.dados);
          this.total.set(resposta.total);
          this.carregando.set(false);
        },
        error: () => {
          this.erro.set('Não foi possível carregar os beneficiários. Tente novamente.');
          this.carregando.set(false);
        },
      });
  }

  aplicarFiltros(): void {
    this.pagina = 1;
    this.carregar();
  }

  irParaPagina(pagina: number): void {
    if (pagina < 1) {
      return;
    }

    this.pagina = pagina;
    this.carregar();
  }

  nomeDoPlano(planoId: string): string {
    return this.planos().find((p) => p.id === planoId)?.nome ?? planoId;
  }

  abrirCadastro(): void {
    this.beneficiarioEmEdicao.set(null);
    this.mostrandoFormulario.set(true);
  }

  abrirEdicao(beneficiario: Beneficiario): void {
    this.beneficiarioEmEdicao.set(beneficiario);
    this.mostrandoFormulario.set(true);
  }

  fecharFormulario(salvou: boolean): void {
    this.mostrandoFormulario.set(false);

    if (salvou) {
      this.carregar();
    }
  }

  excluir(beneficiario: Beneficiario): void {
    if (!confirm(`Excluir ${beneficiario.nome_completo}?`)) {
      return;
    }

    this.servico.excluir(beneficiario.id).subscribe({
      next: () => this.carregar(),
      error: () => this.erro.set('Não foi possível excluir. Tente novamente.'),
    });
  }

  get totalDePaginas(): number {
    return Math.max(1, Math.ceil(this.total() / this.tamanho));
  }
}
