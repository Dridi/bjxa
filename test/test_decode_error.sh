#!/bin/sh
#
# Copyright (C) 2018  Dridi Boukelmoune
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.

. "$(dirname "$0")"/test_setup.sh

_ ----------
_ Empty file
_ ----------

expect_error "bjxa_fread_header" bjxa decode </dev/null

_ ---------------
_ Unreadable file
_ ---------------

expect_error "bjxa_fread_header" bjxa decode <&1

_ ------------------
_ Wrong magic number
_ ------------------

mk_hex <<EOF
4b574432 | KWD2 (id)
c0680a00 | 682176 (nDataLen)
fc170a00 | 661500 (nSamples)
44ac     | 44100 (nSamplesPerSec)
08       | 8 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ ---
_ EIO
_ ---

mk_hex <<EOF
4b574431 | KWD1 (id)
00000000 | 0 (nDataLen)
fc170a00 | 661500 (nSamples)
44ac     | 44100 (nSamplesPerSec)
08       | 8 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ ----------
_ ENOSAMPLES
_ ----------

mk_hex <<EOF
4b574431 | KWD1 (id)
c0680a00 | 682176 (nDataLen)
00000000 | 0 (nSamples)
44ac     | 44100 (nSamplesPerSec)
08       | 8 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ ---------------
_ ETOOMANYSAMPLES
_ ---------------

mk_hex <<EOF
4b574431 | KWD1 (id)
c0680a00 | 682176 (nDataLen)
a1bb0d00 | 900001 (nSamplesPerSec)
44ac     | 44100 (nSamplesPerSec)
08       | 8 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ -----------------
_ ENOTENOUGHSAMPLES
_ -----------------

mk_hex <<EOF
4b574431 | KWD1 (id)
c0680a00 | 682176 (nDataLen)
2a000000 | 42 (nSamplesPerSec)
44ac     | 44100 (nSamplesPerSec)
08       | 8 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ -----------
_ EWAYTOOSLOW
_ -----------

mk_hex <<EOF
4b574431 | KWD1 (id)
c0680a00 | 682176 (nDataLen)
fc170a00 | 661500 (nSamples)
0000     | 0 (nSamplesPerSec)
08       | 8 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ ------------------------------------------
_ Compressed blocks do not match data length
_ ------------------------------------------

mk_hex <<EOF
4b574431 | KWD1 (id)
23000000 | 35 (nDataLen)
10000000 | 16 (nSamples)
44ac     | 44100 (nSamplesPerSec)
08       | 8 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ -------------------
_ Unknown compression
_ -------------------

mk_hex <<EOF
4b574431 | KWD1 (id)
c0680a00 | 682176 (nDataLen)
fc170a00 | 661500 (nSamples)
44ac     | 44100 (nSamplesPerSec)
0c       | 12 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ -----------
_ Home studio
_ -----------

mk_hex <<EOF
4b574431 | KWD1 (id)
c0680a00 | 682176 (nDataLen)
fc170a00 | 661500 (nSamples)
44ac     | 44100 (nSamplesPerSec)
08       | 8 (nBits)
05       | 5 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
EOF

expect_error "bjxa_fread_header" bjxa decode <"$WORK_DIR"/bin

_ --------------------------
_ Invalid mono block profile
_ --------------------------

mk_hex <<EOF
4b574431 | KWD1 (id)
19000000 | 25 (nDataLen)
20000000 | 32 (nSamples)
44ac     | 44100 (nSamplesPerSec)
06       | 6 (nBits)
01       | 1 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
ff       | block profile (invalid)
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
EOF

expect_error "bjxa_decode" bjxa decode <"$WORK_DIR"/bin

_ -----------------------------------
_ Invalid right channel block profile
_ -----------------------------------

mk_hex <<EOF
4b574431 | KWD1 (id)
32000000 | 50 (nDataLen)
20000000 | 32 (nSamples)
44ac     | 44100 (nSamplesPerSec)
06       | 6 (nBits)
02       | 2 (nChannels)
00000000 | 0 (nLoopPtr)
0000     | 0 (befL[0])
0000     | 0 (befL[1])
0000     | 0 (befR[0])
0000     | 0 (befR[1])
00000000 | 0 (pad)
00       | block profile
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
ff       | block profile (invalid)
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
00000000 | block data
EOF

expect_error "bjxa_decode" bjxa decode <"$WORK_DIR"/bin
