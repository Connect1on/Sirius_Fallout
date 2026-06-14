## Misfits Add: Examine flavor text for Critical/SoftCritical mob states.

# Full Critical — completely incapacitated, urgent.
misfits-crit-examine-critical = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-BE($target) } синее лицо и едва дышит — срочно требуется медицинская помощь![/color]

# SoftCritical — conscious but badly hurt, still needs help.
misfits-crit-examine-softcritical = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-BE($target) } сильно ранен и изо всех сил пытается удержаться на ногах { POSS-ADJ($target) } — { SUBJECT($target) } нужно немного подлечиться.[/color]