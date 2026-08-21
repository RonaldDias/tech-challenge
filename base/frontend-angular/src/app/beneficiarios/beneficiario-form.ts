import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Plano } from '../planos/plano';
import { Beneficiario, StatusBeneficiario } from './beneficiario';
import { BeneficiarioServico } from './beneficiario-servico';

@Component({
  selector: 'app-beneficiario-form',
  imports: [FormsModule],
  templateUrl: './beneficiario-form.html',
  styleUrl: './beneficiario-form.css',
})
export class BeneficiarioForm {
  private readonly servico = inject(BeneficiarioServico);

  @Input() planos: Plano[] = [];
  @Input() beneficiario: Beneficiario | null = null;
  @Output() fechar = new EventEmitter<boolean>();

  nomeCompleto = this.beneficiario?.nome_completo ?? '';
  cpf = this.beneficiario?.cpf ?? '';
  dataNascimento = this.beneficiario?.data_nascimento ?? '';
  planoId = this.beneficiario?.plano_id ?? '';
  status: StatusBeneficiario = this.beneficiario?.status ?? 'ATIVO';

  readonly salvando = signal(false);
  readonly erro = signal<string | null>(null);

  get ehEdicao(): boolean {
    return this.beneficiario !== null;
  }

  salvar(): void {
    this.erro.set(null);

    const cpfLimpo = this.cpf.replace(/\D/g, '');

    if (!this.nomeCompleto || this.nomeCompleto.trim().length < 3) {
      this.erro.set('Nome completo precisa ter ao menos 3 caracteres.');
      return;
    }

    if (!this.ehEdicao && cpfLimpo.length !== 11) {
      this.erro.set('CPF precisa ter 11 dígitos.');
      return;
    }

    if (!this.dataNascimento || new Date(this.dataNascimento) >= new Date()) {
      this.erro.set('Data de nascimento precisa estar no passado.');
      return;
    }

    if (!this.planoId) {
      this.erro.set('Selecione um plano.');
      return;
    }

    this.salvando.set(true);

    const operacao = this.ehEdicao
      ? this.servico.atualizar(this.beneficiario!.id, {
          nome_completo: this.nomeCompleto,
          data_nascimento: this.dataNascimento,
          plano_id: this.planoId,
          status: this.status,
        })
      : this.servico.criar({
          nome_completo: this.nomeCompleto,
          cpf: cpfLimpo,
          data_nascimento: this.dataNascimento,
          plano_id: this.planoId,
        });

    operacao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.fechar.emit(true);
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.erro.set(resposta.error?.mensagem ?? 'Não foi possível salvar. Confira os dados.');
      },
    });
  }

  cancelar(): void {
    this.fechar.emit(false);
  }
}
