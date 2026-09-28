document.addEventListener("DOMContentLoaded", function () {

    const bars = Array.from(
        document.querySelectorAll(".hour-bar")
    );

    const maxValue = Math.max(
        ...bars.map(bar =>
            Number(bar.dataset.value || 0)
        ),
        1
    );

    bars.forEach(bar => {

        const value =
            Number(bar.dataset.value || 0);

        const height =
            value === 0
                ? 2
                : Math.max(
                    8,
                    (value / maxValue) * 105
                );

        bar.style.height =
            `${height}px`;
    });

});