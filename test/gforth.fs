\ test some gforth extension words

\ Copyright (C) 2003,2004,2005,2006,2007,2009,2011,2015,2016 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation, either version 3
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program. If not, see http://www.gnu.org/licenses/.

require ./tester.fs
decimal

\ test whether FILL corrupts FTOS (regression)

variable x
T{ 3e x 1 cells 'a' fill -> 3e }T

\ f>str-rdp (then f.rdp and f>buf-rdb should also be ok)

{  12.3456789e 7 3 1 f>str-rdp s"  12.346" str= -> true }
{  12.3456789e 7 4 1 f>str-rdp s" 12.3457" str= -> true }
{ -12.3456789e 7 4 1 f>str-rdp s" -1.23E1" str= -> true }
{      0.0996e 7 3 1 f>str-rdp s"   0.100" str= -> true }
{      0.0996e 7 3 3 f>str-rdp s" 9.96E-2" str= -> true }
{    999.9994e 7 3 1 f>str-rdp s" 999.999" str= -> true }
{    999.9996e 7 3 1 f>str-rdp s" 1.000E3" str= -> true }
{       -1e-20 5 2 1 f>str-rdp s" *****"   str= -> true }

\ 0x hex number conversion, or not

decimal
{ 0x10 -> 16 }
{ 0X10 -> 16 }
36 base !
{ 0x10 -> x10 }
decimal
{ 'a' -> 97 }
{ 'A  -> 65 }
{ 1. '1 -> 1. 49 }

\ REPRESENT has no trailing 0s even for inf and nan

{  1e 0e f/ pad 16 represent drop 2drop pad 15 + c@ '0 = -> false }
{  0e 0e f/ pad 16 represent drop 2drop pad 15 + c@ '0 = -> false }
{ -1e 0e f/ pad 16 represent drop 2drop pad 15 + c@ '0 = -> false }

\ TRY and friends

: 0<-throw ( n -- )
    0< throw ;

: try-test1 ( n1 -- n2 )
    try
        dup 0<-throw
        iferror
            2drop 25
        then
        1+
    endtry ;

{ -5 try-test1 -> 26 }
{ 5  try-test1 ->  6 }

: try-test2 ( n1 -- n2 )
    try
        0
    restore
        drop 1+ dup 0<-throw
    endtry ;

{ -5 try-test2 -> 0 }
{  5 try-test2 -> 6 }

: try-test3 ( n1 -- n2 )
    try
        dup 0<-throw
    endtry-iferror
        2drop 10
    else
        1+
    then ;

{ -5 try-test3 -> 10 }
{  5 try-test3 ->  6 }

\ fcopysign

t{  5e  1e fcopysign ->  5e }t
t{ -5e  1e fcopysign ->  5e }t
t{  5e -1e fcopysign -> -5e }t
t{ -5e -1e fcopysign -> -5e }t
\ tests involving -0e?

\ ?of nextcase contof

: mysgn ( n1 -- n2 )
    case
	dup 0< ?of drop -1 endof
	dup 0> ?of drop 1 endof
	dup
    endcase ;

t{  5 mysgn ->  1 }
t{ -3 mysgn -> -1 }
t{  0 mysgn ->  0 }

: myscan ( addr1 n1 char -- addr2 n2 )
    >r case
	dup 0= ?of endof
        over c@ r@ = ?of endof
        1 /string
        next-case
    rdrop ;

t{ s" dhfa;jfsdk" 2dup ';' myscan 2swap 4 /string d= -> true }
t{ s" abcdef" 2dup 'g' myscan 2swap 6 /string d= -> true }


: gcd ( n1 n2 -- n )
    case
	2dup > ?of tuck - contof
	2dup < ?of over - contof
    endcase ;

t{ 48 42 gcd -> 6 }
t{ 42 48 gcd -> 6 }


: x1 ( u -- u u1 ... un )
    case
	dup
	1 of endof
        dup 1 and ?of 3 * 1+ contof
        2/
    next-case ;

t{ 7 x1 -> 7 22 11 34 17 52 26 13 40 20 10 5 16 8 4 2 1 }t

\ recognizer tests

T{ 4 STACK constant RS -> }T

T{ :noname 1 ;  :noname 2 ;  :noname 3  ; recognizer r:1 -> }T
T{ :noname 10 ; :noname 20 ; :noname 30 ; recognizer r:2 -> }T

\ really stupid: 1 character length or 2 characters
T{ : rec:1 NIP 1 = IF r:1 ELSE R:FAIL THEN ; -> }T
T{ : rec:2 NIP 2 = IF r:2 ELSE R:FAIL THEN ; -> }T

T{ r:1 R>INT EXECUTE  -> 1 }T
T{ r:1 R>COMP EXECUTE -> 2 }T
T{ r:1 R>POST EXECUTE -> 3 }T

\ set and get methods
T{ 0 RS SET-STACK -> }T
T{ RS GET-STACK -> 0 }T

T{ ' rec:1 1 RS SET-STACK -> }T
T{ RS GET-STACK -> ' rec:1 1 }T

T{ ' rec:1 ' rec:2 2 RS SET-STACK -> }T
T{ RS GET-STACK -> ' rec:1 ' rec:2 2 }T

\ testing MAP-RECOGNIZERS
T{         0 RS SET-STACK -> }T
T{ S" 1"     RS MAP-RECOGNIZER   -> R:FAIL }T
T{ ' rec:1 1 RS SET-STACK -> }T
T{ S" 1"     RS MAP-RECOGNIZER   -> R:1 }T
T{ S" 10"    RS MAP-RECOGNIZER   -> R:FAIL }T
T{ ' rec:2 ' rec:1 2 RS SET-STACK -> }T
T{ S" 10"    RS MAP-RECOGNIZER   -> R:2 }T

\ extended synonym behaviour
t{ create coc1 -> }t
t{ synonym coc2 coc1 -> }t
t{ coc2 -> coc1 }t
t{ : coc3 coc2 ; -> }t
t{ coc3 -> coc1 }t
t{ ' coc2 -> ' coc1 }t \ so >body obviously works

t{ defer cod1 -> }t
t{ synonym cod2 cod1 -> }t
t{ ' true is cod2 -> }t
t{ cod2 -> true }t
t{ cod1 -> true }t
t{ action-of cod2 -> ' true }t

\ synonym behaviour for umethods; SOURCE is a umethod
t{ synonym source2 source -> }t
t{ ' source2 -> ' source }t
t{ action-of source2 -> action-of source }t
