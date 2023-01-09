use bulletproofs::PedersenGens;
use curve25519_dalek_ng::{ristretto::RistrettoPoint, scalar::Scalar};
use std::slice;

use crate::deref;

#[no_mangle]
pub extern "C" fn pedersen_gens_default() -> *mut PedersenGens {
    Box::into_raw(Box::default())
}

#[no_mangle]
pub unsafe extern "C" fn pedersen_gens_new(
    g: *const RistrettoPoint,
    h: *const RistrettoPoint,
) -> *mut PedersenGens {
    let g = *g;
    let h = *h; // consider if Rust steals these Points?
    Box::into_raw(Box::new(PedersenGens {
        B: g,
        B_blinding: h,
    }))
}

#[no_mangle]
pub unsafe extern "C" fn pedersen_gens_commit_bytes(
    this: *mut PedersenGens,
    value: *const u8,
    blinding: *const u8,
) -> *mut RistrettoPoint {
    let this = &*this;
    let value = slice::from_raw_parts(value, 32);
    let blinding = slice::from_raw_parts(blinding, 32);

    let value = Scalar::from_bytes_mod_order(value.try_into().unwrap());
    let blinding = Scalar::from_bytes_mod_order(blinding.try_into().unwrap());

    Box::into_raw(Box::new(this.commit(value, blinding)))
}

#[no_mangle]
pub extern "C" fn pedersen_gens_B(this: *const PedersenGens) -> *const RistrettoPoint {
    let this = &deref!(this);
    Box::into_raw(Box::new(this.B))
}

#[no_mangle]
pub extern "C" fn pedersen_gens_B_blinding(this: *const PedersenGens) -> *const RistrettoPoint {
    let this = &deref!(this);
    Box::into_raw(Box::new(this.B_blinding))
}

#[no_mangle]
pub unsafe extern "C" fn pedersen_gens_commit(
    this: *mut PedersenGens,
    value: *const Scalar,
    blinding: *const Scalar,
) -> *mut RistrettoPoint {
    let this = deref!(this);
    Box::into_raw(Box::new(this.commit(*value, *blinding)))
}

#[no_mangle]
pub extern "C" fn pedersen_gens_free(this: *mut PedersenGens) {
    if this.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(this));
    }
}
