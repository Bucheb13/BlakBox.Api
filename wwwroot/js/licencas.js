(function () {

    "use strict";


    // ============================================================
    // HISTÓRICO
    // ============================================================

    window.alternarHistorico = function (id) {

        const linha =
            document.getElementById(
                "historico-" + id
            );

        if (!linha) {
            return;
        }

        linha.classList.toggle("aberto");
    };


    // ============================================================
    // TEMPO RESTANTE
    // ============================================================

    function atualizarTextoTempo() {

        const elementos =
            document.querySelectorAll(
                "[data-vencimento]"
            );

        const agora =
            new Date();

        elementos.forEach(function (elemento) {

            const vencimentoTexto =
                elemento.getAttribute(
                    "data-vencimento"
                );

            const toleranciaTexto =
                elemento.getAttribute(
                    "data-valida-ate"
                );

            if (!vencimentoTexto) {
                return;
            }

            const vencimento =
                new Date(vencimentoTexto);

            if (isNaN(vencimento.getTime())) {
                return;
            }

            const validaAte =
                toleranciaTexto
                    ? new Date(toleranciaTexto)
                    : null;


            // ====================================================
            // DENTRO DO PERÍODO CONTRATADO
            // ====================================================

            if (agora <= vencimento) {

                const diferenca =
                    vencimento.getTime() -
                    agora.getTime();

                const minutos =
                    Math.floor(
                        diferenca /
                        (1000 * 60)
                    );

                const dias =
                    Math.floor(
                        minutos / 1440
                    );

                const horas =
                    Math.floor(
                        (minutos % 1440) / 60
                    );

                const minutosRestantes =
                    minutos % 60;


                if (dias > 3) {

                    elemento.className =
                        "tempo-badge tempo-ok";

                }
                else {

                    elemento.className =
                        "tempo-badge tempo-alerta";

                }


                if (dias > 0) {

                    elemento.textContent =
                        "🟢 " +
                        dias +
                        " dia(s), " +
                        horas +
                        "h";

                }
                else if (horas > 0) {

                    elemento.textContent =
                        "🟠 " +
                        horas +
                        "h " +
                        minutosRestantes +
                        "min";

                }
                else {

                    elemento.textContent =
                        "🟠 " +
                        Math.max(
                            1,
                            minutosRestantes
                        ) +
                        "min";
                }

                return;
            }


            // ====================================================
            // PERÍODO DE TOLERÂNCIA
            // ====================================================

            if (
                validaAte &&
                !isNaN(validaAte.getTime()) &&
                agora <= validaAte
            ) {

                const diferenca =
                    validaAte.getTime() -
                    agora.getTime();

                const minutos =
                    Math.floor(
                        diferenca /
                        (1000 * 60)
                    );

                const dias =
                    Math.floor(
                        minutos / 1440
                    );

                const horas =
                    Math.floor(
                        (minutos % 1440) / 60
                    );

                const minutosRestantes =
                    minutos % 60;


                elemento.className =
                    "tempo-badge tempo-tolerancia";


                if (dias > 0) {

                    elemento.textContent =
                        "🟠 Tolerância: " +
                        dias +
                        " dia(s), " +
                        horas +
                        "h";

                }
                else if (horas > 0) {

                    elemento.textContent =
                        "🟠 Tolerância: " +
                        horas +
                        "h " +
                        minutosRestantes +
                        "min";

                }
                else {

                    elemento.textContent =
                        "🟠 Tolerância: " +
                        Math.max(
                            1,
                            minutosRestantes
                        ) +
                        "min";
                }

                return;
            }


            // ====================================================
            // VENCIDA
            // ====================================================

            const vencida =
                agora.getTime() -
                vencimento.getTime();

            const horas =
                Math.floor(
                    vencida /
                    (1000 * 60 * 60)
                );

            const dias =
                Math.floor(
                    horas / 24
                );


            elemento.className =
                "tempo-badge tempo-vencido";


            if (dias > 0) {

                elemento.textContent =
                    "🔴 Vencida há " +
                    dias +
                    " dia(s)";

            }
            else {

                elemento.textContent =
                    "🔴 Vencida há " +
                    Math.max(
                        1,
                        horas
                    ) +
                    " hora(s)";
            }

        });
    }


    // ============================================================
    // CONFIRMAÇÕES
    // ============================================================

    window.confirmarAtivacao = function (id) {

        const plano =
            document.querySelector(
                'select[form="form-ativar-' +
                id +
                '"]'
            );

        if (!plano || !plano.value) {

            alert(
                "Selecione um plano antes de ativar a licença."
            );

            return false;
        }

        return confirm(
            "Ativar esta licença agora?\n\n" +
            "O plano selecionado será usado para " +
            "calcular o período da licença."
        );
    };


    window.confirmarReativacao = function () {

        return confirm(
            "Reativar esta licença?\n\n" +
            "A data de início, vencimento e plano atuais " +
            "serão preservados."
        );
    };


    window.confirmarRenovacao = function () {

        return confirm(
            "Renovar esta licença?\n\n" +
            "A renovação respeitará o plano atual."
        );
    };


    window.confirmarInativacao = function () {

        return confirm(
            "Inativar esta licença?\n\n" +
            "O acesso da oficina será bloqueado imediatamente. " +
            "O cadastro e o histórico serão preservados."
        );
    };


    window.confirmarCancelamento = function () {

        return confirm(
            "Cancelar esta assinatura?\n\n" +
            "O acesso será bloqueado imediatamente e o " +
            "cancelamento ficará registrado no histórico."
        );
    };


    // ============================================================
    // INICIALIZAÇÃO
    // ============================================================

    document.addEventListener(
        "DOMContentLoaded",
        function () {

            atualizarTextoTempo();

            setInterval(
                atualizarTextoTempo,
                60000
            );

        }
    );

})();